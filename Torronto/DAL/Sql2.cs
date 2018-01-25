using System;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Linq;

namespace Torronto.DAL
{
    public static class Sql2
    {
        [Sql.Function(ServerSideOnly = true)]
        public static string MD5(string str)
        {
            throw new LinqException("Server-side only");
        }

        [Sql.Expression("{0} IS NULL", ServerSideOnly = true)]
        public static bool IsNull(object o)
        {
            throw new LinqException("Server-side only");
        }

        [ExpressionMethod("IsNullOrFalseExpression")]
        public static bool IsNullOrFalse(bool b)
        {
            throw new NotImplementedException();
        }

        public static Expression<Func<bool, bool>> IsNullOrFalseExpression()
        {
            return (value) => value == null || value == false;
        }
    }
}