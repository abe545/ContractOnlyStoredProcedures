using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    internal static class MethodGenerator
    {
        private static readonly MethodInfo create = typeof(StoredProcedure).GetMethod(nameof(StoredProcedure.Create), new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo execute = typeof(StoredProcedure).GetMethod(nameof(StoredProcedure.Execute), new[] { typeof(IDbConnection), typeof(int) });
        private static readonly MethodInfo executeAsync = typeof(StoredProcedure).GetMethod(nameof(StoredProcedure.ExecuteAsync), new[] { typeof(IDbConnection), typeof(int) });
        private static readonly Lazy<MethodInfo> withReturnValue = new Lazy<MethodInfo>(() => typeof(StoredProcedureExtensions).GetMethod(nameof(StoredProcedureExtensions.WithReturnValue)));
        
        public static Expression CreateStoredProcedure(
            this MethodInfo method,
            string schema,
            out List<ParameterExpression> parameters,
            out List<ParameterExpression> closureParameters,
            out List<Expression>          afterExecutionSteps)
        {
            Contract.Requires(method != null);
            Contract.Ensures(Contract.ValueAtReturn(out parameters) != null);
            Contract.Ensures(Contract.Result<Expression>() != null && 
                             typeof(StoredProcedure).IsAssignableFrom(Contract.Result<Expression>().Type));

            var name = method.Name;
            var returnType = method.ReturnType;
            var isAsync = typeof(Task).IsAssignableFrom(returnType);
            var spType = typeof(StoredProcedure);

            parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToList();

            if (isAsync && parameters.Any(p => p.IsByRef))
                throw new NotSupportedException("Can not return an output parameter from a stored procedure asynchronously.");

            // naming conventions for async methods mean we should drop the async from the stored procedure name
            if (isAsync && name.EndsWith("Async", StringComparison.InvariantCultureIgnoreCase))
                name = name.Substring(0, name.Length - 5);

            // var sp = StoredProcedure.Create(schema, name);
            Expression sp = Expression.Call(create, Expression.Constant(schema), Expression.Constant(name));

            // We treat method that returns an int as if it wants the value from the return statement
            if (returnType != typeof(int) && returnType != typeof(Task<int>) && returnType != typeof(void) && returnType != typeof(Task))
            {
                if (isAsync)
                    returnType = returnType.GetGenericArguments().Single();

                if (returnType.IsGenericType)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(returnType))
                    {
                        returnType = returnType.GetGenericArguments().Single();
                        // sp = sp.WithResults<returnType>();
                        sp = Expression.Call(GetWithResults(returnType), sp);
                        spType = typeof(StoredProcedure<>).MakeGenericType(returnType);
                    }
                    else if (returnType.Namespace == "System" && returnType.Name.StartsWith("Tuple"))
                    {
                        var returnTypes = returnType.GetGenericArguments()
                                                    .Select(t =>
                                                    {
                                                        if (!typeof(IEnumerable).IsAssignableFrom(t) || !t.IsGenericType)
                                                            throw new NotSupportedException("Can only return a tuple with every type being a generic IEnumerable");

                                                        return t.GetGenericArguments().Single();
                                                    })
                                                    .ToArray();
                        sp = Expression.Call(GetWithResults(returnTypes), sp);

                        spType = typeof(StoredProcedure).Assembly
                                                        .GetTypes()
                                                        .First(t => t.Name == $"StoredProcedure`{returnTypes.Length}")
                                                        .MakeGenericType(returnTypes);
                    }
                }
                else
                    sp = Expression.Call(GetWithResults(returnType), sp);
            }

            closureParameters = new List<ParameterExpression>();
            afterExecutionSteps = new List<Expression>();
            foreach (var p in parameters)
            {
                if (p.IsByRef)
                {
                    var outputValue = Expression.Parameter(p.Type, "o");
                    var outputTemp = Expression.Parameter(p.Type, $"{p.Name}_Temp");
                    var setter = Expression.Lambda(typeof(Action<>).MakeGenericType(p.Type), Expression.Assign(outputTemp, outputValue), outputValue);
                    var gwio = GetWithInputOutput(spType, p.Type);
                    var args = new Expression[] { sp, Expression.Constant(p.Name), p, setter }.Concat(
                        gwio.GetParameters().Skip(4).Select(ip => Expression.Constant(ip.DefaultValue, ip.ParameterType))).ToArray();
                    sp = Expression.Call(gwio, args);

                    closureParameters.Add(outputTemp);
                    afterExecutionSteps.Add(Expression.Assign(p, outputTemp));
                }
                else
                    sp = Expression.Call(GetWithInput(spType, p.Type), sp, Expression.Constant(p.Name), p);
            }

            return sp;
        }

        public static Expression CreateMethod(
            this MethodInfo method,
            Expression dbConnection,
            Expression timeout,
            out List<ParameterExpression> parameters,
            string schema = "dbo")
        {
            Contract.Requires(method != null);
            Contract.Requires(dbConnection != null);
            Contract.Requires(timeout != null);
            Contract.Ensures(Contract.ValueAtReturn(out parameters) != null);
            Contract.Ensures(Contract.Result<Expression>() != null);

            var execMethod = execute;
            var execAsyncMethod = executeAsync;
            var returnType = method.ReturnType;
            var isAsync = typeof(Task).IsAssignableFrom(returnType);

            List<ParameterExpression> temporaryVariables;
            List<Expression> afterExecuteMethods;

            var sp = method.CreateStoredProcedure(schema, out parameters, out temporaryVariables, out afterExecuteMethods);
            if (sp.Type != typeof(StoredProcedure))
            {
                execMethod = sp.Type.GetMethod(nameof(StoredProcedure.Execute), new[] { typeof(IDbConnection), typeof(int) });
                execAsyncMethod = sp.Type.GetMethod(nameof(StoredProcedure.ExecuteAsync), new[] { typeof(IDbConnection), typeof(int) });
            }

            if (returnType == typeof(int) || returnType == typeof(Task<int>))
            {
                // we are going to treat this as a return value, so we need to create a lambda that will
                // lift the value out of the extension method, and into a variable we can use in our 
                // method body.
                var retVal = Expression.Parameter(typeof(int), "returnValue");
                var i = Expression.Parameter(typeof(int), "i");
                var retValMethod = Expression.Lambda<Action<int>>(Expression.Assign(retVal, i), i);

                sp = Expression.Call(withReturnValue.Value.MakeGenericMethod(typeof(StoredProcedure)), sp, retValMethod);

                if (isAsync)
                {
                    // this is trickier. We need to add a continuation to the task being returned, and first check
                    // to see if it is faulted. If so, throw the exception. If not, return the lifted variable.
                    var t = Expression.Parameter(typeof(Task), "t");
                    var cont = Expression.Lambda<Func<Task, int>>(
                        Expression.Block(typeof(int),
                            Expression.IfThen
                            (
                                Expression.Equal(
                                    Expression.Property(t, typeof(Task).GetProperty(nameof(Task.Status))),
                                    Expression.Constant(TaskStatus.Faulted)),
                                Expression.Throw(Expression.Property(t, typeof(Task).GetProperty(nameof(Task.Exception))))
                            ),
                            retVal
                        ), t);

                    sp = Expression.Call(sp, executeAsync, dbConnection, timeout);
                    sp = Expression.Call(sp, GetContinuationMethod<Task, int>(), cont);
                    return Expression.Block(returnType, new ParameterExpression[] { retVal }, sp);
                }

                sp = Expression.Call(sp, execute, dbConnection, timeout);
                return Expression.Block(returnType, new ParameterExpression[] { retVal }, sp, retVal);
            }

            if (isAsync)
            {
                sp = Expression.Call(sp, execAsyncMethod, dbConnection, timeout);
            }
            else
            {
                sp = Expression.Call(sp, execMethod, dbConnection, timeout);

                if (afterExecuteMethods.Count > 0)
                {
                    if (returnType != typeof(void))
                    {
                        var result = Expression.Parameter(returnType, "result");
                        temporaryVariables.Insert(0, result);
                        afterExecuteMethods.Insert(0, Expression.Assign(result, sp));
                        afterExecuteMethods.Add(result);
                    }
                    else
                        afterExecuteMethods.Insert(0, sp);

                    return Expression.Block(returnType, temporaryVariables, afterExecuteMethods);
                }
            }

            return sp;
        }

        private static MethodInfo GetContinuationMethod<TTask, TResult>()
            where TTask : Task
        {
            return typeof(TTask).GetMethods()
                                .Where(mi => mi.Name == nameof(Task.ContinueWith) && mi.IsGenericMethodDefinition)
                                .OrderBy(mi => mi.GetParameters().Length)
                                .First()
                                .MakeGenericMethod(typeof(TResult));
        }

        private static MethodInfo GetWithResults(params Type[] typeParameters)
        {
            return GetExtensionMethod(nameof(StoredProcedureExtensions.WithResults), typeParameters);
        }

        private static MethodInfo GetWithInput(params Type[] typeParameters)
        {
            return GetExtensionMethod(nameof(StoredProcedureExtensions.WithParameter), typeParameters, types => types.Count() == 3);
        }

        private static MethodInfo GetWithInputOutput(params Type[] typeParameters)
        {
            return GetExtensionMethod(nameof(StoredProcedureExtensions.WithInputOutputParameter), typeParameters, types => types.Count() == 7);
        }

        private static MethodInfo GetExtensionMethod(string methodName, Type[] typeParameters, Func<IEnumerable<Type>, bool> parameterTypeFilter = null)
        {
            var methods = typeof(StoredProcedureExtensions).GetMethods()
                                                           .Where(mi => mi.Name == methodName)
                                                           .Where(mi => mi.GetGenericArguments().Length == typeParameters.Length);

            if (parameterTypeFilter != null)
                methods = methods.Where(mi => parameterTypeFilter(mi.GetParameters().Select(pi => pi.ParameterType)));

            return methods.Single().MakeGenericMethod(typeParameters);
        }
    }
}
