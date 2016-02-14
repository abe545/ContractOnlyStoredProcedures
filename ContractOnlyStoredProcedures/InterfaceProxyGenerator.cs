using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeOnlyStoredProcedure
{
    internal class InterfaceProxyGenerator<T>
    {
        static readonly Type dynamicType;

        public static T Create(IDbConnection connection, int timeout)
        {
            return (T)Activator.CreateInstance(dynamicType, connection, timeout);
        }

        static InterfaceProxyGenerator()
        {
            var type = typeof(T);
            var asmName = type.Assembly.GetName().Name + ".Generated";
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(asmName), AssemblyBuilderAccess.Run);
            var mod = asm.DefineDynamicModule(asmName);
            var dynType = mod.DefineType($"{type.Namespace}.{type.Name}Proxy", TypeAttributes.Class | TypeAttributes.Public);
            dynType.AddInterfaceImplementation(type);

            var dbc = dynType.DefineField("connection", typeof(IDbConnection), FieldAttributes.InitOnly | FieldAttributes.Private);
            var to = dynType.DefineField("timeout", typeof(int), FieldAttributes.InitOnly | FieldAttributes.Private);

            DefineConstructor(dynType, dbc, to);

            var connExpr = Expression.Parameter(typeof(IDbConnection), "connection");
            var toExpr = Expression.Parameter(typeof(int), "timeout");

            foreach (var mi in type.GetMethods())
            {
                var paramTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                var staticParamTypes = new[] { typeof(IDbConnection), typeof(int) }.Concat(paramTypes).ToArray();

                var staticMethodBuilder = DefineStaticMethod(mod, dynType, connExpr, toExpr, mi, staticParamTypes);
                DefineInstanceMethod(dynType, dbc, to, mi, paramTypes, staticMethodBuilder);
            }

            dynamicType = dynType.CreateType();
        }

        private static void DefineConstructor(TypeBuilder dynType, FieldBuilder dbc, FieldBuilder to)
        {
            var ctor = dynType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IDbConnection), typeof(int) });
            var il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, dbc);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, to);
            il.Emit(OpCodes.Ret);
        }

        private static MethodBuilder DefineStaticMethod(
            ModuleBuilder mod, 
            TypeBuilder dynType, 
            ParameterExpression connExpr, 
            ParameterExpression toExpr, 
            MethodInfo mi, 
            Type[] staticParamTypes)
        {
            var staticMethodBuilder = dynType.DefineMethod(
                                $"{mi.Name}Proxy",
                                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                                CallingConventions.Standard,
                                mi.ReturnType,
                                staticParamTypes);

            List<ParameterExpression> inputParams;
            var method = mi.CreateMethod(connExpr, toExpr, out inputParams);
            var delegateType = GetDelegateType(mi, mod, staticParamTypes);

            inputParams.Insert(0, connExpr);
            inputParams.Insert(1, toExpr);

            var lambda = Expression.Lambda(delegateType, method, inputParams.ToArray());
            lambda.CompileToMethod(staticMethodBuilder);
            return staticMethodBuilder;
        }

        static Type GetDelegateType(MethodInfo method, ModuleBuilder modBuilder, Type[] paramTypes)
        {
            // the 8 parameter types are in mscorlib, so Type.GetType actually works.
            // TODO: Test the 9-16 type parameter versions found in System.Core
            if (paramTypes.Length <= 8 && paramTypes.All(t => !t.IsByRef))
            {
                // we can easily use the built-in Func or Action types
                if (method.ReturnType != typeof(void))
                {
                    var delType = Type.GetType($"System.Func`{paramTypes.Length + 1}");
                    return delType.MakeGenericType(paramTypes.Concat(new[] { method.ReturnType }).ToArray());
                }
                else
                    return Type.GetType($"System.Action`{paramTypes.Length}").MakeGenericType(paramTypes);
            }

            // Create a delegate that has the same signature as the method we would like to hook up to
            var typeBuilder = modBuilder.DefineType($"{method.Name}Delegate",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass,
                typeof(MulticastDelegate));

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard,
                new Type[] { typeof(object), typeof(IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            
            // Define the Invoke method for the delegate
            var methodBuilder = typeBuilder.DefineMethod(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, 
                method.ReturnType, 
                paramTypes);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            // bake it!
            return typeBuilder.CreateType();
        }

        static MethodBuilder DefineInstanceMethod(
            TypeBuilder dynType,
            FieldBuilder dbc,
            FieldBuilder to,
            MethodInfo mi,
            Type[] paramTypes,
            MethodBuilder staticMethodBuilder)
        {
            ILGenerator il;
            var methodBuilder = dynType.DefineMethod(
                  mi.Name,
                  MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final,
                  CallingConventions.Standard,
                  mi.ReturnType,
                  paramTypes);

            il = methodBuilder.GetILGenerator();

            // get the db connection
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, dbc);

            // get the timeout
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, to);

            // load all the parameters
            for (int i = 0; i < paramTypes.Length; i++)
            {
                // we offset the argument by 1, as arg 0 is this
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, i + 1);
                        break;
                }
            }

            il.Emit(OpCodes.Call, staticMethodBuilder);
            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }
    }
}
