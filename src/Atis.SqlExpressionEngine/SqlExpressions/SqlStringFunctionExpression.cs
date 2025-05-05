using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlStringFunctionExpression : SqlExpression
    {
        public override SqlExpressionType NodeType => SqlExpressionType.StringFunction;
        public SqlExpression StringExpression { get; }
        public SqlExpression[] Arguments { get; }
        public SqlStringFunction StringFunction { get; }

        public SqlStringFunctionExpression(SqlStringFunction stringFunction, SqlExpression stringExpression, SqlExpression[] arguments)
        {
            this.StringFunction = stringFunction;
            this.StringExpression = stringExpression ?? throw new ArgumentNullException(nameof(stringExpression));
            this.Arguments = arguments;
        }

        protected internal override SqlExpression Accept(SqlExpressionVisitor sqlExpressionVisitor)
        {
            return sqlExpressionVisitor.VisitSqlStringFunction(this);
        }

        public SqlStringFunctionExpression Update(SqlExpression stringExpression, SqlExpression[] arguments)
        {
            if (stringExpression == this.StringExpression && ArgumentsSame(arguments))
            {
                return this;
            }
            return new SqlStringFunctionExpression(this.StringFunction, stringExpression, arguments);
        }

        private bool ArgumentsSame(SqlExpression[] arguments)
        {
            if (this.Arguments?.Length != arguments?.Length)
                return false;

            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] != this.Arguments[i])
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            var argumentsToString = this.Arguments != null ? string.Join(", ", this.Arguments.Select(arg => arg.ToString())) : string.Empty;
            if (argumentsToString.Length > 0)
            {
                argumentsToString = ", " + argumentsToString;
            }
            return $"{this.StringFunction}({this.StringExpression}{argumentsToString})";
        }
    }
}
