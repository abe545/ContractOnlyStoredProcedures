using System;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute that controls the name and schema of a stored procedure to execute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class StoredProcedureAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the stored procedure that will be called when the method is executed. If not set, will default to the method name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the schema of the stored procedure that will be called when the method is executed. If not set, will default to dbo.
        /// </summary>
        public string Schema { get; set; }
    }
}
