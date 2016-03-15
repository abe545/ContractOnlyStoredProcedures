using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Machine.Specifications;

#if NET40
namespace CodeOnlyStoredProcedure.Tests.Net40
#else
namespace CodeOnlyStoredProcedure.Tests
#endif
{
    [Subject("GenerateProxy")]
    public class when_connection_is_null
    {
        static IDbConnection Subject;
        static Exception Exception;

        Establish context = () => Subject = null;
        Because of = () => Exception = Catch.Exception(() => Subject.GenerateProxy<IEmptyDataStoreTest>());
        It should_fail = () => Exception.Should().NotBeNull("because an exception should be thrown");
        It should_be_ArgumentNullException = () => Exception.Should().BeOfType(typeof(ArgumentNullException));
    }

    [Subject("GenerateProxy")]
    public class when_interface_has_no_methods
    {
        static IDbConnection Subject;
        static IEmptyDataStoreTest Proxy;

        Establish context = () => Subject = Moq.Mock.Of<IDbConnection>();
        Because of = () => Proxy = Subject.GenerateProxy<IEmptyDataStoreTest>();
        It should_not_be_null = () => Proxy.Should().NotBeNull();
    }

    [Subject("GenerateProxy")]
    public class when_interface_has_one_method_with_no_arguments
    {
        static IDbConnection Subject;
        static IOneMethodDataStoreTest Proxy;

        Establish context = () => Subject = Moq.Mock.Of<IDbConnection>();
        Because of = () => Proxy = Subject.GenerateProxy<IOneMethodDataStoreTest>();
        It should_not_be_null = () => Proxy.Should().NotBeNull();
    }

    [Subject("GenerateProxy")]
    public class when_interface_is_typical_database_proxy
    {
        static IDbConnection Subject;
        static IDataStoreTest Proxy;

        Establish context = () => Subject = Moq.Mock.Of<IDbConnection>();
        Because of = () => Proxy = Subject.GenerateProxy<IDataStoreTest>();
        It should_not_be_null = () => Proxy.Should().NotBeNull();
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_returns_string_results
    {
        static IDbConnection Subject;
        static IEnumerable<string> Results;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IDataStoreTest>();
            Results = proxy.usp_GetNames();
        };

        It should_have_returned_expected_results = () => Results.ShouldAllBeEquivalentTo(new[] { "Abe Heidebrecht", "Abe Lincoln", "Abe Simpson" });
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_async_method_on_database_that_returns_string_results
    {
        static IDbConnection Subject;
        static AwaitResult<IEnumerable<string>> Results;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IDataStoreTest>();
            Results = proxy.usp_GetNamesAsync().Await();
        };

        It should_have_returned_expected_results = () => Results.AsTask.Result.ShouldAllBeEquivalentTo(new[] { "Abe Heidebrecht", "Abe Lincoln", "Abe Simpson" });
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_returns_value_from_input
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IDataStoreTest>();
            Result = proxy.usp_GetId("Abe");
        };

        It should_have_returned_length_of_input = () => Result.Should().Be(3);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_async_method_on_database_that_returns_value_from_input
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IDataStoreTest>();
            Result = proxy.usp_GetIdAsync("Abe").Await();
        };

        It should_have_returned_length_of_input = () => Result.Should().Be(3);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_returns_value_from_input_as_out_parameter
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IReturnValueTest>();
            proxy.usp_GetId("Abe", out Result);
        };

        It should_have_returned_length_of_input = () => Result.Should().Be(3);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_has_an_output_parameter
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IOutputTest>();
            proxy.usp_TimesTwo(42, out Result);
        };

        It should_have_set_output_value_to_double_the_input_value = () => Result.Should().Be(84);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_uses_complex_parameter
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IWithComplexArgumentTest>();
            Result = proxy.usp_GetId(new Parameter { Name = "Abe" });
        };

        It should_have_set_output_value_to_double_the_input_value = () => Result.Should().Be(3);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_async_generated_method_on_database_that_uses_complex_parameter
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IWithComplexArgumentTest>();
            Result = proxy.usp_GetIdAsync(new Parameter { Name = "Abe" }).Await();
        };

        It should_have_set_output_value_to_double_the_input_value = () => Result.Should().Be(3);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_generated_method_on_database_that_takes_a_table_valued_parameter
    {
        static IDbConnection Subject;
        static IEnumerable<SimplePerson> Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<ITableValuedParameterTest>();
            Result = proxy.usp_ReturnPeople(new[] { new SimplePerson { Name = "Abe", Age = 42 }, new SimplePerson { Name = "foo", Age = 1000 } });
        };

        It should_return_the_same_number_of_items_passed_in = () => Result.Should().HaveCount(2, "because 2 items were passed into the procedure");
        It should_return_the_same_age_for_abe_as_was_passed_in = () => Result.Single(p => p.Name == "Abe").Age.Should().Be(42);
        It should_return_the_same_age_for_foo_as_was_passed_in = () => Result.Single(p => p.Name == "foo").Age.Should().Be(1000);
    }

    [Subject("GenerateProxy"), Tags("SmokeTest")]
    public class when_calling_async_generated_method_on_database_that_takes_a_table_valued_parameter
    {
        static IDbConnection Subject;
        static IEnumerable<SimplePerson> Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<ITableValuedParameterTest>();
            Result = proxy.usp_ReturnPeopleAsync(new[] { new SimplePerson { Name = "Abe", Age = 42 }, new SimplePerson { Name = "foo", Age = 1000 } }).Await().AsTask.Result;
        };

        It should_return_the_same_number_of_items_passed_in = () => Result.Should().HaveCount(2, "because 2 items were passed into the procedure");
        It should_return_the_same_age_for_abe_as_was_passed_in = () => Result.Single(p => p.Name == "Abe").Age.Should().Be(42);
        It should_return_the_same_age_for_foo_as_was_passed_in = () => Result.Single(p => p.Name == "foo").Age.Should().Be(1000);
    }

    public class when_calling_method_on_database_that_is_renamed_through_the_StoredProcedureAttribute
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IAttributedTest>();
            Result = proxy.Foo("Abe");
        };

        It should_have_returned_length_of_input = () => Result.Should().Be(3);
    }

    public class when_calling_an_async_method_on_database_that_is_renamed_through_the_StoredProcedureAttribute
    {
        static IDbConnection Subject;
        static int Result;

        Establish context = () => Subject = new SqlConnection(SmokeDb.Connection);
        Because of = () =>
        {
            var proxy = Subject.GenerateProxy<IAttributedTest>();
            Result = proxy.Bar("Abe").Await();
        };

        It should_have_returned_length_of_input = () => Result.Should().Be(3);
    }

    internal interface IEmptyDataStoreTest { }
    public interface IOneMethodDataStoreTest
    {
        void usp_NoArgumentsOrReturn();
    }
    public interface IDataStoreTest
    {
        IEnumerable<string> usp_GetNames();
        Task<IEnumerable<string>> usp_GetNamesAsync();

        int usp_GetId(string name);

        Task<int> usp_GetIdAsync(string name);
    }

    public interface IOutputTest
    {
        void usp_TimesTwo(int input, out int output);
    }

    public interface IReturnValueTest
    {
        void usp_GetId(string name, out int returnValue);
    }

    public class Parameter { public string Name { get; set; } }
    public interface IWithComplexArgumentTest
    {
        int usp_GetId(Parameter parm);
        Task<int> usp_GetIdAsync(Parameter parm);
    }

    [TableValuedParameter(TableName = "ReturnMe")]
    public class SimplePerson
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public interface ITableValuedParameterTest
    {
        IEnumerable<SimplePerson> usp_ReturnPeople(IEnumerable<SimplePerson> people);
        Task<IEnumerable<SimplePerson>> usp_ReturnPeopleAsync(IEnumerable<SimplePerson> people);
    }

    public interface IAttributedTest
    {
        [StoredProcedure(Name = "usp_GetId")]
        int Foo(string name);
        [StoredProcedure(Name = "usp_GetId")]
        Task<int> Bar(string name);
    }
}
