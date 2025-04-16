using Atis.SqlExpressionEngine.Abstractions;
using Atis.SqlExpressionEngine.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine.Internal
{
    /// <summary>
    ///     <para>
    ///         Responsible for converting an array of SqlExpressions (possibly nested through anonymous object initializers)
    ///         into a flat list of SqlColumnExpression with hierarchical ModelPath preserved. This is useful for building
    ///         SELECT projections in SQL when the LINQ query uses anonymous type projections like:
    ///     </para>
    ///     <para>
    ///         CAUTION: This class is intended for internal use only. It should not be used directly by end users.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class is used in the <c>NewExpressionConverter</c> and <c>MemberInitExpressionConverter</c>, 
    ///         primarily to flatten a nested anonymous type into a single flat list of columns with full model path resolution. 
    ///         Example:
    ///     </para>
    ///     <para>
    ///         <code>
    ///             q.Select(x => new { o1 = new {  f1 = new { C1 = x.Table1.Col1, C2 = x.Table1.Col2 }, 
    ///                                             f2 = x.Table3.Col3 }, 
    ///                                 o2 = x.Table4.Col4 });
    ///         </code>
    ///     </para>
    ///     <para>
    ///         When <c>NewExpressionConverter</c> converts this expression, it recursively traverses each <c>NewExpression</c>, converting the
    ///         innermost <c>NewExpression</c> first. In the example above, <c>new { C1 = x.Table1.Col1, C2 = x.Table1.Col2 }</c>
    ///         is converted first. It will be transformed into <c>SqlExpression[]</c> and <c>MemberNames[]</c> like this:
    ///     </para>
    ///     <para>
    ///         <code>
    ///             SqlExpression[0] = SqlDataSourceColumnExpression (x.Table1.Col1)
    ///             SqlExpression[1] = SqlDataSourceColumnExpression (x.Table1.Col2)
    ///             MemberNames[0] = "C1"
    ///             MemberNames[1] = "C2"
    ///         </code>
    ///     </para>
    ///     <para>
    ///         <c>NewExpressionConverter</c> will then call the <c>Create</c> method of this class and pass these two parameters.
    ///         In return, it will receive a list of <c>SqlColumnExpression</c> like this:
    ///     </para>
    ///     <para>
    ///         <code>
    ///             SqlColumnExpression[0] = SqlColumnExpression (x.Table1.Col1, "C1", ModelPath = "C1")
    ///             SqlColumnExpression[1] = SqlColumnExpression (x.Table1.Col2, "C2", ModelPath = "C2")
    ///         </code>
    ///     </para>
    ///     <para>
    ///         After that, control returns to the outer <c>NewExpression</c> conversion, that is,
    ///         <c>new { f1 = ..., f2 = ... }</c>. Here, <c>SqlExpression[]</c> and <c>MemberNames[]</c>
    ///         will look like this:
    ///     </para>
    ///     <para>
    ///         <code>
    ///             SqlExpression[0] = SqlCollectionExpression (contains SqlColumnExpression instances created in the previous step) 
    ///             SqlExpression[1] = SqlDataSourceColumnExpression (x.Table3.Col3)
    ///             MemberNames[0] = "f1"
    ///             MemberNames[1] = "f2"
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Again, <c>NewExpressionConverter</c> will convert this to <c>SqlColumnExpression</c>. When <c>Flatten</c> is called,
    ///         it recursively goes to the leaf nodes first, passing along the <c>parentPath</c> during each recursive call.
    ///     </para>
    ///     <para>
    ///         <code>
    ///             1. Entry parentPath = Empty
    ///             2. First element is a collection; recursive call with parentPath = "f1"
    ///             3. Entry with parentPath = "f1"
    ///             4. For each <c>SqlColumnExpression</c>, a copy is created with <c>parentPath</c> appended to the current model path.
    ///                So if the original ModelPath was "C1", it becomes "f1.C1".
    ///         </code>
    ///     </para>
    /// </remarks>
    public class ProjectionCreator
    {
        private readonly ISqlExpressionFactory sqlFactory;

        public ProjectionCreator(ISqlExpressionFactory sqlFactory)
        {
            this.sqlFactory = sqlFactory;
        }

        /// <summary>
        ///     <para>
        ///         Creates a flat list of SqlColumnExpression by wrapping a single SqlCollectionExpression. This is a convenience
        ///         overload that wraps the input in an array and calls the main Create method.
        ///     </para>
        /// </summary>
        /// <param name="sqlCollection">The SqlCollectionExpression representing a collection of columns or nested structures.</param>
        /// <returns>A flat list of SqlColumnExpression preserving model paths.</returns>
        public IEnumerable<SqlColumnExpression> Create(SqlCollectionExpression sqlCollection)
        {
            return Create(new[] { sqlCollection }, new[] { (string)null });
        }

        /// <summary>
        ///     <para>
        ///         Converts an array of SqlExpressions and their corresponding member names (from anonymous type initializers)
        ///         into a flat list of SqlColumnExpression with full ModelPath resolution.
        ///     </para>
        ///     <para>
        ///         Each SqlExpression is either a simple column, a data source reference, or a nested SqlCollectionExpression,
        ///         which itself is recursively flattened and appended with the correct model path prefix.
        ///     </para>
        /// </summary>
        /// <param name="expressions">An array of SqlExpressions representing projection parts.</param>
        /// <param name="memberNames">Corresponding member names to use as column aliases and path elements.</param>
        /// <returns>A flat IEnumerable of SqlColumnExpression with full hierarchical model paths.</returns>
        public IEnumerable<SqlColumnExpression> Create(SqlExpression[] expressions, string[] memberNames)
        {
            if (expressions is null || expressions.Length == 0)
                throw new ArgumentNullException(nameof(expressions));
            if (memberNames is null || memberNames.Length == 0)
                throw new ArgumentNullException(nameof(memberNames));
            if (expressions.Length != memberNames.Length)
                throw new ArgumentException($"Length of {nameof(expressions)} and {nameof(memberNames)} must be equal.");

            var normalizedExpressions = new List<SqlColumnExpression>();

            for (int i = 0; i < expressions.Length; i++)
            {
                var expr = expressions[i];

                if (expr is SqlCollectionExpression collection)
                {
                    if (collection.SqlExpressions is null)
                        throw new InvalidOperationException($"SqlExpressions of the SqlCollectionExpression at index {i} is null.");

                    // Below method call will look at each entry and assumes that there are
                    // SqlColumnExpression or SqlDataSourceReferenceExpression instances only,
                    // then it converts to a new SqlCollectionExpression instance that contains
                    // only SqlColumnExpression instances.
                    expr = NormalizeSqlCollection(collection);
                }

                normalizedExpressions.Add(this.sqlFactory.CreateColumn(
                    expr,
                    memberNames[i],
                    new ModelPath(memberNames[i])
                ));
            }

            var flatResult = new List<SqlColumnExpression>();
            Flatten(normalizedExpressions, ModelPath.Empty, flatResult);
            return flatResult;
        }

        private SqlExpression NormalizeSqlCollection(SqlCollectionExpression collection)
        {
            var children = collection.SqlExpressions.ToArray();
            var wrappedChildren = new List<SqlColumnExpression>();

            for (int j = 0; j < children.Length; j++)
            {
                SqlExpression columnExpr;
                string columnAlias;
                ModelPath columnModelPath;

                if (children[j] is SqlDataSourceReferenceExpression dsRef)
                {
                    columnExpr = dsRef;
                    columnAlias = null;
                    columnModelPath = dsRef.Reference.ModelPath;
                }
                else if (children[j] is SqlColumnExpression colExpr)
                {
                    columnExpr = colExpr.ColumnExpression;
                    columnAlias = colExpr.ColumnAlias;
                    columnModelPath = colExpr.ModelPath;
                }
                else
                {
                    throw new InvalidOperationException("Expected SqlDataSourceReferenceExpression or SqlColumnExpression");
                }

                wrappedChildren.Add(this.sqlFactory.CreateColumn(columnExpr, columnAlias, columnModelPath));
            }

            return this.sqlFactory.CreateCollection(wrappedChildren);
        }

        private void Flatten(IEnumerable<SqlColumnExpression> inputs, ModelPath parentPath, List<SqlColumnExpression> output)
        {
            int index = 0;

            foreach (var input in inputs)
            {
                var expr = input.ColumnExpression ??
                    throw new InvalidOperationException($"ColumnExpression at index {index} is null.");

                if (expr is SqlCollectionExpression nestedCollection)
                {
                    if (nestedCollection.SqlExpressions is null)
                        throw new InvalidOperationException($"SqlExpressions of nested SqlCollectionExpression at index {index} is null.");

                    var children = nestedCollection.SqlExpressions.Cast<SqlColumnExpression>().ToArray();

                    Flatten(children, input.ModelPath, output);     // recursive call
                }
                else
                {
                    var flattened = this.sqlFactory.CreateColumn(
                        input.ColumnExpression,
                        input.ColumnAlias,
                        parentPath.Append(input.ModelPath)
                    );
                    output.Add(flattened);
                }

                index++;
            }
        }
    }
}
