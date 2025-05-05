using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine
{
    public class SqlExpressionReplacementVisitor : SqlExpressionVisitor
    {
        private readonly SqlExpression toFind;
        private SqlExpression toReplace;
        private SqlExpression searchIn;

        public SqlExpressionReplacementVisitor(SqlExpression toFind, SqlExpression toReplace)
        {
            this.toFind = toFind ?? throw new ArgumentNullException(nameof(toFind));
            this.toReplace = toReplace;
        }

        public static SqlExpression FindAndReplace(SqlExpression searchIn, SqlExpression oldExpression, SqlParameterExpression newExpression)
        {
            var visitor = new SqlExpressionReplacementVisitor(oldExpression, newExpression);
            return visitor.Visit(searchIn);
        }

        public static SqlExpressionReplacementVisitor Find(SqlExpression toFind)
        {
            return new SqlExpressionReplacementVisitor(toFind, null);
        }

        public SqlExpressionReplacementVisitor In(SqlExpression searchIn)
        {
            this.searchIn = searchIn ?? throw new ArgumentNullException(nameof(searchIn));
            return this;
        }

        public SqlExpression ReplaceWith(SqlExpression replaceWith)
        {
            if (this.searchIn is null)
                throw new InvalidOperationException("The searchIn expression must be set before replacing.");
            this.toReplace = replaceWith ?? throw new ArgumentNullException(nameof(replaceWith));
            return this.Visit(this.searchIn);
        }

        /// <inheritdoc />
        public override SqlExpression Visit(SqlExpression node)
        {
            if (node == null)
                return null;
            if (node == this.toFind)
                return this.toReplace;
            return base.Visit(node);
        }
    }
}
