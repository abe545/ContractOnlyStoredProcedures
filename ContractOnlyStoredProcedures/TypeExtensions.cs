using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace CodeOnlyStoredProcedure
{
    internal static class TypeExtensions
    {
        internal static bool IsEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            return t.IsArray || typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType;
        }

        internal static Type GetEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            if (t.IsArray)
                return t.GetElementType();

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return t.GetGenericArguments().Single();

            return t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Where(i => i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                    .Select(i => i.GetGenericArguments().First())
                    .FirstOrDefault();
        }
    }
}
