using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Machine.Specifications;
using mspec = Machine.Specifications;
using Moq;

#if NET40
namespace CodeOnlyStoredProcedure.Tests.Net40
#else
namespace CodeOnlyStoredProcedure.Tests
#endif
{
    public abstract class MethodGeneratorTestBase
    {
        protected static ParameterExpression dbConnectionExpression = Expression.Parameter(typeof(IDbConnection), "dbConnection");
        protected static ParameterExpression timeoutExpression = Expression.Parameter(typeof(int), "timeout");
        protected static List<ParameterExpression> ParameterExpressions;

        protected static T GetAndVerifyCreateStoredProcedureResultExpression<T>(MethodInfo mi, string schema = "dbo")
        {
            List<ParameterExpression> _;
            List<Expression> __;
            var sp = mi.CreateStoredProcedure(schema, out ParameterExpressions, out _, out __);
            sp.Should().NotBeNull();

            if (_.Count > 0)
                return Expression.Lambda<T>(Expression.Block(_, sp), ParameterExpressions).Compile();

            return Expression.Lambda<T>(sp, ParameterExpressions).Compile();
        }

        protected static T GetAndVerifyMethod<T>(MethodInfo mi)
        {
            var method = mi.CreateMethod(dbConnectionExpression, timeoutExpression, out ParameterExpressions);
            method.Should().NotBeNull();
            return Expression.Lambda<T>(method, new[] { dbConnectionExpression, timeoutExpression }.Concat(ParameterExpressions)).Compile();
        }

        protected static IDbConnection CreateCommand(out Mock<IDbCommand> command)
        {
            return CreateCommand(SetupCommand, out command);
        }

        protected static IDbConnection CreateCommand(Action<Mock<IDbCommand>> setupCommand, out Mock<IDbCommand> command)
        {
            command = new Mock<IDbCommand>();
            setupCommand(command);

            var conn = new Mock<IDbConnection>();
            conn.Setup(c => c.CreateCommand())
                .Returns(command.Object);

            return conn.Object;
        }

        private static void SetupCommand(Mock<IDbCommand> command)
        {
            command.SetupAllProperties();
            command.SetupGet(c => c.Parameters).Returns(Mock.Of<IDataParameterCollection>());
        }

        protected static void SetupStringCommand(Mock<IDbCommand> command, IEnumerable<string> results)
        {
            SetupCommand(command);

            var reader = new Mock<IDataReader>();
            reader.Setup(rdr => rdr.FieldCount).Returns(1);
            reader.Setup(rdr => rdr.IsDBNull(0)).Returns(false);
            reader.SetupSequence(rdr => rdr.Read())
                  .Returns(true)
                  .Returns(true)
                  .Returns(false);
            reader.Setup(rdr => rdr.GetFieldType(0)).Returns(typeof(string));
            var seq = reader.SetupSequence(rdr => rdr.GetString(0));
            foreach (var s in results)
                seq = seq.Returns(s);

            command.Setup(c => c.ExecuteReader()).Returns(reader.Object);
        }
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_no_arguments_and_no_results : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IEmptyMethods).GetMethod(nameof(IEmptyMethods.Empty)))();
        };

        mspec.It should_be_named_Empty = () => StoredProcedure.Name.Should().Be("Empty");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_no_arguments_and_no_results : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IEmptyMethods).GetMethod(nameof(IEmptyMethods.EmptyAsync)))();
        };

        mspec.It should_be_named_Empty = () => StoredProcedure.Name.Should().Be("Empty");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_no_arguments_and_a_int_result : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValue)))();
        };

        mspec.It should_be_named_WithReturnValue = () => StoredProcedure.Name.Should().Be("WithReturnValue");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_no_arguments_and_a_int_result : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValueAsync)))();
        };

        mspec.It should_be_named_WithReturnValue = () => StoredProcedure.Name.Should().Be("WithReturnValue");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_no_arguments_and_string_results : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure<string>>>(
                typeof(IWithStringResultsMethods).GetMethod(nameof(IWithStringResultsMethods.WithResultSet)))();
        };

        mspec.It should_be_named_WithResultSet = () => StoredProcedure.Name.Should().Be("WithResultSet");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_no_arguments_and_string_results : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure<string>>>(
                typeof(IWithStringResultsMethods).GetMethod(nameof(IWithStringResultsMethods.WithResultSetAsync)))();
        };

        mspec.It should_be_named_WithResultSet = () => StoredProcedure.Name.Should().Be("WithResultSet");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_no_arguments_and_multiple_result_sets : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure<string, int, float, double, byte, long, short>>>(
                typeof(IMultipleResultSetMethods).GetMethod(nameof(IMultipleResultSetMethods.WithMultipleResultSets)))();
        };

        mspec.It should_be_named_WithMultipleResultSets = () => StoredProcedure.Name.Should().Be("WithMultipleResultSets");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_no_arguments_and_multiple_result_sets : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure<string, int, float, double, byte, long, short>>>(
                typeof(IMultipleResultSetMethods).GetMethod(nameof(IMultipleResultSetMethods.WithMultipleResultSetsAsync)))();
        };

        mspec.It should_be_named_WithMultipleResultSets = () => StoredProcedure.Name.Should().Be("WithMultipleResultSets");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
    }
    
    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_no_arguments_and_invalid_result_sets : MethodGeneratorTestBase
    {
        static Exception Exception;
        Because of = () =>
        {
            Exception = Catch.Exception(() => GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IMultipleResultSetMethods).GetMethod(nameof(IMultipleResultSetMethods.ShouldFail)))());
        };

        mspec.It should_throw_NotSupportedException = () => Exception.Should().BeOfType(typeof(NotSupportedException));
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_no_arguments_and_invalid_result_sets : MethodGeneratorTestBase
    {
        static Exception Exception;
        Because of = () =>
        {
            Exception = Catch.Exception(() => GetAndVerifyCreateStoredProcedureResultExpression<Func<StoredProcedure>>(
                typeof(IMultipleResultSetMethods).GetMethod(nameof(IMultipleResultSetMethods.ShouldFailAsync)))());
        };

        mspec.It should_throw_NotSupportedException = () => Exception.Should().BeOfType(typeof(NotSupportedException));
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_an_argument_and_no_result_sets : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<string, StoredProcedure>>(
                typeof(IWithArgumentMethods).GetMethod(nameof(IWithArgumentMethods.WithArgument)))("foo");
        };

        mspec.It should_be_named_WithArgument = () => StoredProcedure.Name.Should().Be("WithArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithArgument](@name = 'foo')");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_an_argument_and_no_result_sets : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<string, StoredProcedure>>(
                typeof(IWithArgumentMethods).GetMethod(nameof(IWithArgumentMethods.WithArgumentAsync)))("bar");
        };

        mspec.It should_be_named_WithArgument = () => StoredProcedure.Name.Should().Be("WithArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithArgument](@name = 'bar')");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_an_output_parameter : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            int outItem;
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<WithOutputAndNoResults>(
                typeof(IWIthOutputArgumentMethods).GetMethod(nameof(IWIthOutputArgumentMethods.WithOutputArgument)))(out outItem);
        };

        mspec.It should_be_named_WithOutputArgument = () => StoredProcedure.Name.Should().Be("WithOutputArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithOutputArgument]([Out] @result)");
    }
    
    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_an_output_parameter_which_is_returnValue : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            int returnValue;
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<WithOutputAndNoResults>(
                typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValueAsArgument)))(out returnValue);
        };

        mspec.It should_be_named_WithOutputArgument = () => StoredProcedure.Name.Should().Be("WithReturnValueAsArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithReturnValueAsArgument](@returnValue)");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_an_output_parameter : MethodGeneratorTestBase
    {
        static Exception Exception;
        Because of = () =>
        {
            int outItem;
            Exception = Catch.Exception(() => GetAndVerifyCreateStoredProcedureResultExpression<WithOutputAndNoResults>(
                typeof(IWIthOutputArgumentMethods).GetMethod(nameof(IWIthOutputArgumentMethods.ShouldThrow)))(out outItem));
        };

        mspec.It should_throw_NotSupportedException = () => Exception.Should().BeOfType(typeof(NotSupportedException));
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_a_stored_procedure_that_has_a_complex_input_argument : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<Input, StoredProcedure>>(
                typeof(IWithComplexArgumentMethods).GetMethod(nameof(IWithComplexArgumentMethods.WithArgument)))(new Input { Name = "foo" });
        };

        mspec.It should_be_named_WithArgument = () => StoredProcedure.Name.Should().Be("WithArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithArgument](@Name = 'foo')");
    }

    [Subject("CreateStoredProcedure")]
    public class when_creating_an_async_stored_procedure_that_has_a_complex_input_argument : MethodGeneratorTestBase
    {
        static StoredProcedure StoredProcedure;
        Because of = () =>
        {
            StoredProcedure = GetAndVerifyCreateStoredProcedureResultExpression<Func<Input, StoredProcedure>>(
                typeof(IWithComplexArgumentMethods).GetMethod(nameof(IWithComplexArgumentMethods.WithArgumentAsync)))(new Input { Name = "foo" });
        };

        mspec.It should_be_named_WithArgument = () => StoredProcedure.Name.Should().Be("WithArgument");
        mspec.It should_have_dbo_schema = () => StoredProcedure.Schema.Should().Be("dbo");
        mspec.It should_show_parameter_values_from_ToString = () => StoredProcedure.ToString().Should().Be("[dbo].[WithArgument](@Name = 'foo')");
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_no_arguments_and_no_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;

        Establish context = () => Connection = CreateCommand(out Command);
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Action<IDbConnection, int>>(typeof(IEmptyMethods).GetMethod(nameof(IEmptyMethods.Empty)));
            method(Connection, 30);
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[Empty]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Subject("CreateMethod")]
    public class when_creating_an_async_method_that_has_no_arguments_and_no_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;

        Establish context = () => Connection = CreateCommand(out Command);
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Func<IDbConnection, int, Task>>(typeof(IEmptyMethods).GetMethod(nameof(IEmptyMethods.EmptyAsync)));
            method(Connection, 30).Await();
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[Empty]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_no_arguments_and_an_int_result : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static Mock<IDbDataParameter> ReturnParameter;
        static IDbConnection Connection;
        static int Result;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            ReturnParameter = new Mock<IDbDataParameter>();
            ReturnParameter.SetupAllProperties();
            ReturnParameter.Object.Value = 1;
            Command.Setup(c => c.CreateParameter()).Returns(ReturnParameter.Object);
        };
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Func<IDbConnection, int, int>>(typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValue)));
            Result = method(Connection, 30);
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithReturnValue]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
        mspec.It should_have_setup_the_parameter_type = () => ReturnParameter.Object.DbType.Should().Be(DbType.Int32);
        mspec.It should_have_setup_the_parameter_direction = () => ReturnParameter.Object.Direction.Should().Be(ParameterDirection.ReturnValue);
        mspec.It should_have_returned_the_result = () => Result.Should().Be(1);
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_return_value_via_output_parameter : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static Mock<IDbDataParameter> ReturnParameter;
        static IDbConnection Connection;
        static int Result;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            ReturnParameter = new Mock<IDbDataParameter>();
            ReturnParameter.SetupAllProperties();
            ReturnParameter.Object.Value = 1;
            Command.Setup(c => c.CreateParameter()).Returns(ReturnParameter.Object);
        };
        Because of = () =>
        {
            var method = GetAndVerifyMethod<WithOutputAndNoResultsStatic>(
                typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValueAsArgument)));
            method(Connection, 30, out Result);
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithReturnValueAsArgument]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
        mspec.It should_have_setup_the_parameter_type = () => ReturnParameter.Object.DbType.Should().Be(DbType.Int32);
        mspec.It should_have_setup_the_parameter_direction = () => ReturnParameter.Object.Direction.Should().Be(ParameterDirection.ReturnValue);
        mspec.It should_have_returned_the_result = () => Result.Should().Be(1);
    }

    [Subject("CreateMethod")]
    public class when_creating_an_async_method_that_has_no_arguments_and_an_int_result : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static Mock<IDbDataParameter> ReturnParameter;
        static IDbConnection Connection;
        static int Result;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            ReturnParameter = new Mock<IDbDataParameter>();
            ReturnParameter.SetupAllProperties();
            ReturnParameter.Object.Value = 1;
            Command.Setup(c => c.CreateParameter()).Returns(ReturnParameter.Object);
        };
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Func<IDbConnection, int, Task<int>>>(typeof(IWithReturnValueMethods).GetMethod(nameof(IWithReturnValueMethods.WithReturnValueAsync)));
            Result = method(Connection, 30).Await();
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithReturnValue]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
        mspec.It should_have_setup_the_parameter_type = () => ReturnParameter.Object.DbType.Should().Be(DbType.Int32);
        mspec.It should_have_setup_the_parameter_direction = () => ReturnParameter.Object.Direction.Should().Be(ParameterDirection.ReturnValue);
        mspec.It should_have_returned_the_result = () => Result.Should().Be(1);
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_no_arguments_and_string_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;
        static IEnumerable<string> Results;

        Establish context = () =>
        {
            Connection = CreateCommand(c => SetupStringCommand(c, new[] { "foo", "bar" }), out Command);
        };
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Func<IDbConnection, int, IEnumerable<string>>>(typeof(IWithStringResultsMethods).GetMethod(nameof(IWithStringResultsMethods.WithResultSet)));
            Results = method(Connection, 30);
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithResultSet]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteReader(), Times.Once());
        mspec.It should_have_returned_the_results = () => Results.ShouldAllBeEquivalentTo(new[] { "foo", "bar" });
    }

    [Subject("CreateMethod")]
    public class when_creating_an_async_method_that_has_no_arguments_and_string_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;
        static AwaitResult<IEnumerable<string>> Results;

        Establish context = () =>
        {
            Connection = CreateCommand(c => SetupStringCommand(c, new[] { "foo", "bar" }), out Command);
        };
        Because of = () =>
        {
            var method = GetAndVerifyMethod<Func<IDbConnection, int, Task<IEnumerable<string>>>>(typeof(IWithStringResultsMethods).GetMethod(nameof(IWithStringResultsMethods.WithResultSetAsync)));
            Results = method(Connection, 30).Await();
        };
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithResultSet]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteReader(), Times.Once());
        mspec.It should_have_returned_the_results = () => Results.AsTask.Result.ShouldAllBeEquivalentTo(new[] { "foo", "bar" });
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_an_argument_and_no_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;
        static Mock<IDbDataParameter> InputParameter;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            InputParameter = new Mock<IDbDataParameter>();
            InputParameter.SetupAllProperties();
            Command.Setup(c => c.CreateParameter()).Returns(InputParameter.Object);
        };

        Because of = () =>
        {
            List<ParameterExpression> expressions;
            var method = typeof(IWithArgumentMethods).GetMethod(nameof(IWithArgumentMethods.WithArgument)).CreateMethod(
                dbConnectionExpression,
                timeoutExpression, 
                out expressions);
            Expression.Lambda<Action<IDbConnection, string, int>>(method, dbConnectionExpression, expressions.Single(), timeoutExpression)
                      .Compile()
                      .Invoke(Connection, "foo", 30);
        };
        mspec.It should_set_input_parameter_type_to_string = () => InputParameter.Object.DbType.Should().Be(DbType.String);
        mspec.It should_set_input_parameter_value_to_foo = () => InputParameter.Object.Value.Should().Be("foo");
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithArgument]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Subject("CreateMethod")]
    public class when_creating_an_async_method_that_has_an_argument_and_no_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;
        static Mock<IDbDataParameter> InputParameter;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            InputParameter = new Mock<IDbDataParameter>();
            InputParameter.SetupAllProperties();
            Command.Setup(c => c.CreateParameter()).Returns(InputParameter.Object);
        };

        Because of = () =>
        {
            List<ParameterExpression> expressions;
            var method = typeof(IWithArgumentMethods).GetMethod(nameof(IWithArgumentMethods.WithArgumentAsync)).CreateMethod(
                dbConnectionExpression,
                timeoutExpression,
                out expressions);
            Expression.Lambda<Func<IDbConnection, string, int, Task>>(method, dbConnectionExpression, expressions.Single(), timeoutExpression)
                      .Compile()
                      .Invoke(Connection, "foo", 30)
                      .Await();
        };
        mspec.It should_set_input_parameter_type_to_string = () => InputParameter.Object.DbType.Should().Be(DbType.String);
        mspec.It should_set_input_parameter_value_to_foo = () => InputParameter.Object.Value.Should().Be("foo");
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithArgument]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Subject("CreateMethod")]
    public class when_creating_a_method_that_has_a_complex_argument_and_no_results : MethodGeneratorTestBase
    {
        static Mock<IDbCommand> Command;
        static IDbConnection Connection;
        static Mock<IDbDataParameter> InputParameter;

        Establish context = () =>
        {
            Connection = CreateCommand(out Command);
            InputParameter = new Mock<IDbDataParameter>();
            InputParameter.SetupAllProperties();
            Command.Setup(c => c.CreateParameter()).Returns(InputParameter.Object);
        };

        Because of = () =>
        {
            List<ParameterExpression> expressions;
            var method = typeof(IWithComplexArgumentMethods).GetMethod(nameof(IWithComplexArgumentMethods.WithArgument)).CreateMethod(
                dbConnectionExpression,
                timeoutExpression,
                out expressions);
            Expression.Lambda<Action<IDbConnection, Input, int>>(method, dbConnectionExpression, expressions.Single(), timeoutExpression)
                      .Compile()
                      .Invoke(Connection, new Input { Name = "foo" }, 30);
        };
        mspec.It should_set_input_parameter_type_to_string = () => InputParameter.Object.DbType.Should().Be(DbType.String);
        mspec.It should_set_input_parameter_value_to_foo = () => InputParameter.Object.Value.Should().Be("foo");
        mspec.It should_set_input_parameter_ParameterName_to_Name = () => InputParameter.Object.ParameterName.Should().Be("Name");
        mspec.It should_have_commandText_representing_the_stored_procedure = () => Command.Object.CommandText.Should().Be("[dbo].[WithArgument]");
        mspec.It should_have_executing_the_command_once = () => Command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    public delegate StoredProcedure WithOutputAndNoResults(out int result);
    public delegate void WithOutputAndNoResultsStatic(IDbConnection connection, int timeout, out int result);

    internal interface IEmptyMethods
    {
        void Empty();
        Task EmptyAsync();
    }

    internal interface IWithReturnValueMethods
    {
        int WithReturnValue();
        Task<int> WithReturnValueAsync();
        void WithReturnValueAsArgument(out int returnValue);
    }

    internal interface IWithStringResultsMethods
    {
        IEnumerable<string> WithResultSet();
        Task<IEnumerable<string>> WithResultSetAsync();
    }

    internal interface IMultipleResultSetMethods
    {
        Tuple<IEnumerable<string>, IEnumerable<int>, IEnumerable<float>, IEnumerable<double>, IEnumerable<byte>, IEnumerable<long>, IEnumerable<short>>
            WithMultipleResultSets();
        Task<Tuple<IEnumerable<string>, IEnumerable<int>, IEnumerable<float>, IEnumerable<double>, IEnumerable<byte>, IEnumerable<long>, IEnumerable<short>>>
            WithMultipleResultSetsAsync();
        Tuple<IEnumerable<int>, string> ShouldFail();
        Task<Tuple<IEnumerable<int>, string>> ShouldFailAsync();
    }

    internal interface IWithArgumentMethods
    {
        void WithArgument(string name);
        Task WithArgumentAsync(string name);
    }

    internal interface IWIthOutputArgumentMethods
    {
        void WithOutputArgument(out int result);
        Task ShouldThrow(out int result);
    }

    internal class Input { public string Name { get; set; } }
    internal interface IWithComplexArgumentMethods
    {
        void WithArgument(Input input);
        Task WithArgumentAsync(Input input);
    }
}
