using System;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SqlExpression
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// 
        /// </summary>
        public virtual SqlExpressionType NodeType { get; } = SqlExpressionType.Custom;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        protected internal virtual SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitCustom(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        protected internal virtual SqlExpression VisitChildren(SqlExpressionVisitor visitor)
        {
            return this;
        }
    }
}
