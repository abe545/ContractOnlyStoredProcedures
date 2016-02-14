using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Class that houses extension methods for generating proxies for databases
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Will generate a proxy to a database.
        /// </summary>
        /// <typeparam name="T">The type that describes the contract of the database.</typeparam>
        /// <param name="connection">The connection to use to execute the stored procedure on.</param>
        /// <param name="timeout">The amount of time (in seconds) before a stored procedure times out.</param>
        /// <returns>An implementation of the contract that can be used to execute stored procedures.</returns>
        /// <remarks>Will only work with interfaces at the moment, but future versions will support abstract
        /// classes (which will allow the implementer more control over certain aspects of the proxy).</remarks>
        public static T GenerateProxy<T>(this IDbConnection connection, int timeout = 30) where T : class
        {
            Contract.Requires<ArgumentNullException>(connection != null);
            
            if (!typeof(T).IsInterface)
                throw new NotSupportedException("Can only generate a proxy for interfaces.");
            else
            {
                return InterfaceProxyGenerator<T>.Create(connection, timeout);
            }
        }
    }
}
