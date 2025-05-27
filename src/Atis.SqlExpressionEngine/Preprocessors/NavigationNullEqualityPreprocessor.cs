using Atis.Expressions;
using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.ExpressionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Atis.SqlExpressionEngine.Preprocessors
{
    public class NavigationNullEqualityPreprocessor : ExpressionVisitor, IExpressionPreprocessor
    {
        private readonly IModel model;

        public NavigationNullEqualityPreprocessor(IModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // do nothing
        }

        /// <inheritdoc />
        public Expression Preprocess(Expression expression)
        {
            return this.Visit(expression);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var visited = base.VisitBinary(node);
            if (visited is BinaryExpression binaryExpression)
            {
                if ((binaryExpression.NodeType == ExpressionType.Equal ||
                    binaryExpression.NodeType == ExpressionType.NotEqual) &&
                    binaryExpression.Left is NavigationMemberExpression navigationMemberExpression &&
                    binaryExpression.Right is ConstantExpression constExpression &&
                    constExpression.Value is null)
                {
                    var navigationTableSourceType = navigationMemberExpression.Type;
                    MemberInfo firstPrimaryKey = this.GetFirstPrimaryKey(navigationTableSourceType);
                    if (firstPrimaryKey != null)
                    {
                        // Replace the navigation member with the primary key member
                        Expression newNavigationMember;
                        try
                        {
                            // casting into object is important because this is possible that field type might not be nullable
                            newNavigationMember = Expression.Convert(Expression.MakeMemberAccess(binaryExpression.Left, firstPrimaryKey), typeof(object));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"An error occurred while creating MemberExpression for primary key '{firstPrimaryKey.Name}' on type '{navigationTableSourceType.FullName}', see inner exception for details.", ex);
                        }
                        visited = Expression.MakeBinary(binaryExpression.NodeType, newNavigationMember, constExpression);
                    }
                }
            }
            return visited;
        }

        /// <summary>
        /// Gets the first primary key member of the navigation table source type.
        /// </summary>
        /// <param name="navigationTableSourceType">Type of the navigation table source.</param>
        /// <returns>First primary key member or null if no primary key is found.</returns>
        protected virtual MemberInfo GetFirstPrimaryKey(Type navigationTableSourceType)
        {
            IReadOnlyList<MemberInfo> primaryKeys = this.model.GetPrimaryKeys(navigationTableSourceType);
            return primaryKeys?.FirstOrDefault() ?? this.model.GetColumnMembers(navigationTableSourceType)?.FirstOrDefault();
        }
    }
}
