using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public class SqlMemberInitExpression : SqlQueryShapeExpression
    {
        public SqlMemberInitExpression(IReadOnlyList<SqlMemberAssignment> bindings)
        {
            if (!(bindings?.Count > 0))
                throw new ArgumentNullException(nameof(bindings));
            if (bindings.GroupBy(x => x.MemberName).Any(x => x.Count() > 1))
                throw new ArgumentException("Duplicate member names in bindings", nameof(bindings));

            this.bindings = new List<SqlMemberAssignment>(bindings);
            this.memberDictionary = this.bindings.ToDictionary(x => x.MemberName, x => x.SqlExpression);
        }

        /// <inheritdoc />
        public override SqlExpressionType NodeType => SqlExpressionType.MemberInit;

        private readonly List<SqlMemberAssignment> bindings;
        public IReadOnlyList<SqlMemberAssignment> Bindings => this.bindings;

        private readonly Dictionary<string, SqlExpression> memberDictionary = new Dictionary<string, SqlExpression>();
        public override bool IsScalar => false;
        public override SqlExpression GetScalarExpression() => throw new InvalidOperationException("Not a scalar expression.");

        public override bool TryResolveMember(string memberName, out SqlExpression resolvedExpression)
        {
            if (memberName is null)
                throw new ArgumentNullException(nameof(memberName));
            if (this.memberDictionary.TryGetValue(memberName, out resolvedExpression))
            {
                return true;
            }
            resolvedExpression = null;
            return false;
        }

        public override void AddMemberAssignment(string memberName, SqlExpression assignment, bool projectable)
        {
            if (memberName is null)
                throw new ArgumentNullException(nameof(memberName));
            if (assignment is null)
                throw new ArgumentNullException(nameof(assignment));
            if (this.memberDictionary.ContainsKey(memberName))
                throw new ArgumentException($"Member '{memberName}' already exists in bindings", nameof(memberName));
            this.bindings.Add(new SqlMemberAssignment(memberName, assignment, projectable));
            this.memberDictionary.Add(memberName, assignment);
        }

        public override void RemoveMember(string memberName)
        {
            if (memberName is null)
                throw new ArgumentNullException(nameof(memberName));
            this.memberDictionary.Remove(memberName);
            var binding = this.bindings.FirstOrDefault(x => x.MemberName == memberName)
                            ??
                            throw new ArgumentException($"Member '{memberName}' not found in bindings", nameof(memberName));
            this.bindings.Remove(binding);
        }

        //public void Reset(SqlQueryShapeExpression newShape)
        //{
        //    if (newShape is null)
        //        throw new ArgumentNullException(nameof(newShape));

        //    var updatedShape = CreateCopyOfBinding(newShape);
        //    this.bindings = new List<SqlMemberAssignment>(updatedShape.bindings);
        //    this.memberDictionary = this.bindings.ToDictionary(x => x.MemberName, x => x.SqlExpression);
        //}

        //private SqlQueryShapeExpression CreateCopyOfBinding(SqlQueryShapeExpression queryShape)
        //{
        //    if (this == queryShape)
        //    {
        //        return new SqlQueryShapeExpression(queryShape.bindings.Select(x => new SqlMemberAssignment(x.MemberName, x.SqlExpression)).ToList());
        //    }
        //    else
        //    {
        //        var newBindingList = new List<SqlMemberAssignment>();
        //        var bindingChanged = false;
        //        foreach (var binding in queryShape.Bindings)
        //        {
        //            var bindingToAdd = binding;
        //            if (binding.SqlExpression is SqlQueryShapeExpression qs)
        //            {
        //                var assignment = CreateCopyOfBinding(qs);
        //                if (assignment != binding.SqlExpression)
        //                {
        //                    bindingToAdd = new SqlMemberAssignment(binding.MemberName, assignment);
        //                    bindingChanged = true;
        //                }
        //            }
        //            newBindingList.Add(bindingToAdd);
        //        }
        //        if (bindingChanged)
        //        {
        //            return new SqlQueryShapeExpression(newBindingList);
        //        }
        //    }
        //    return queryShape;
        //}

        //public bool TryGetByMemberName(string memberName, out SqlExpression assignment)
        //{
        //    return this.memberDictionary.TryGetValue(memberName, out assignment);
        //}

        //public SqlMemberAssignment GetByExpression(SqlExpression sqlExpression)
        //{
        //    if (sqlExpression is null)
        //        throw new ArgumentNullException(nameof(sqlExpression));
        //    return this.Bindings.FirstOrDefault(x => x.SqlExpression == sqlExpression);
        //}

        /// <inheritdoc />
        protected internal override SqlExpression Accept(SqlExpressionVisitor visitor)
        {
            return visitor.VisitSqlMemberInit(this);
        }

        public SqlMemberInitExpression Update(IReadOnlyList<SqlMemberAssignment> bindings)
        {
            if (this.Bindings.AllEqual(bindings))
                return this;
            return new SqlMemberInitExpression(bindings);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{string.Join(", ", this.Bindings.Select(x => x.ToString()))}}}";
        }

    }
}
