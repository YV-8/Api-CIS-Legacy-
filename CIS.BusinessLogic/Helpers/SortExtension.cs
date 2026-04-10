using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CIS.BusinessLogic.Helpers
{
    public static class SortExtension
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string[] sorts, Dictionary<string, string> whitelist)
        {
            if (sorts == null || sorts.Length == 0)
                return query;

            bool isFirstSort = true;

            foreach (var sortOption in sorts)
            {
                if (string.IsNullOrWhiteSpace(sortOption)) continue;

                
                var parts = sortOption.Split(',');
                var fieldName = parts[0].Trim();
                var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

               
                if (!whitelist.TryGetValue(fieldName, out string propertyName))
                {
                    
                    throw new ArgumentException($"Unsupported sorting field: '{fieldName}'");
                }

                
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, propertyName);
                var lambda = Expression.Lambda(property, parameter);

                string methodName = isFirstSort
                    ? (direction == "desc" ? "OrderByDescending" : "OrderBy")
                    : (direction == "desc" ? "ThenByDescending" : "ThenBy");

                var methodCallExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.Type },
                    query.Expression,
                    Expression.Quote(lambda)
                );

                query = query.Provider.CreateQuery<T>(methodCallExpression);
                isFirstSort = false;
            }

            return query;
        }
    }
}