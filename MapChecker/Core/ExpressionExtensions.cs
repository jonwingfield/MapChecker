using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MapChecker
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Get the root object of a expression (if it is a MemberExpression) 
        /// e.g. a in a.b.c
        /// </summary>
        public static Expression RootObject(this Expression expression)
        {
            while (expression is MemberExpression)
            {
                expression = ((MemberExpression)expression).Expression;
            }

            return expression;
        }
    }
}
