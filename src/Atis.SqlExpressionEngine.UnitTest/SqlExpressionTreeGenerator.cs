using Atis.SqlExpressionEngine.SqlExpressions;
using System.Xml;

namespace Atis.SqlExpressionEngine.UnitTest
{
    public class SqlExpressionTreeGenerator : SqlExpressionVisitor
    {
        public XmlDocument XmlDoc { get; } = new XmlDocument();
        private XmlElement currentNode;

        public SqlExpressionTreeGenerator()
        {
            this.currentNode = XmlDoc.CreateElement("root");
            XmlDoc.AppendChild(currentNode);
        }

        public static XmlDocument GenerateTree(SqlExpression expression)
        {
            var generator = new SqlExpressionTreeGenerator();
            generator.Generate(expression);
            return generator.XmlDoc;
        }

        public void Generate(SqlExpression expression)
        {
            this.Visit(expression);
        }

        public override SqlExpression Visit(SqlExpression node)
        {
            System.Diagnostics.Debug.Indent();
            var updatedNode = base.Visit(node);
            System.Diagnostics.Debug.Unindent();
            return updatedNode;
        }
        protected override SqlExpression VisitSqlBinaryExpression(SqlBinaryExpression sqlBinaryExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlBinaryExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("NodeType")).Value = sqlBinaryExpression.NodeType.ToString();
            currentNode.AppendChild(elem);
            System.Diagnostics.Debug.WriteLine($"{nameof(SqlBinaryExpression)} ({sqlBinaryExpression.NodeType}):");
            System.Diagnostics.Debug.Indent();
            
            currentNode = XmlDoc.CreateElement("Left");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("Left:");
            this.Visit(sqlBinaryExpression.Left);

            currentNode = XmlDoc.CreateElement("Right");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("Right:");
            this.Visit(sqlBinaryExpression.Right);

            return sqlBinaryExpression;
        }

        protected override SqlExpression VisitSqlParameterExpression(SqlParameterExpression sqlParameterExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlParameterExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("Value")).Value = sqlParameterExpression.Value?.ToString() ?? "null";
            currentNode.AppendChild(elem);
            System.Diagnostics.Debug.WriteLine($"{nameof(SqlParameterExpression)}: {sqlParameterExpression.Value ?? "null"}");
            return sqlParameterExpression;
        }

        protected override SqlExpression VisitSqlDataSourceColumnExpression(SqlDataSourceColumnExpression sqlDataSourceColumnExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlDataSourceColumnExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("Expression")).Value = $"{sqlDataSourceColumnExpression.DataSource.DataSourceAlias}.{sqlDataSourceColumnExpression.ColumnName}";
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlDataSourceColumnExpression)}: {sqlDataSourceColumnExpression.DataSource.DataSourceAlias}.{sqlDataSourceColumnExpression.ColumnName}");
            return sqlDataSourceColumnExpression;
        }

        protected override SqlExpression VisitSqlAliasExpression(SqlAliasExpression sqlAliasExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlAliasExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("ColumnAlias")).Value = sqlAliasExpression.ColumnAlias;
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlAliasExpression)}: {sqlAliasExpression.ColumnAlias}");
            return sqlAliasExpression;
        }

        protected override SqlExpression VisitSqlCollectionExpression(SqlCollectionExpression sqlCollectionExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlCollectionExpression));
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlCollectionExpression)}:");
            System.Diagnostics.Debug.Indent();
            var expressions = sqlCollectionExpression.SqlExpressions?.ToArray() ?? Array.Empty<SqlExpression>();
            var subElem = XmlDoc.CreateElement("SqlExpressions");
            elem.AppendChild(subElem);
            for (var i = 0; i < expressions.Length; i++)
            {
                var expr = expressions[i];
                currentNode = XmlDoc.CreateElement($"SqlExpression");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"SqlExpression {i}:");
                this.Visit(expr);
            }
            System.Diagnostics.Debug.Unindent();
            return sqlCollectionExpression;
        }

        protected override SqlExpression VisitSqlColumnExpression(SqlColumnExpression sqlColumnExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlColumnExpression));
            elem.Attributes.Append(XmlDoc.CreateAttribute("ColumnAlias")).Value = sqlColumnExpression.ColumnAlias;
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("ColumnExpression");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlColumnExpression)} ({sqlColumnExpression.ColumnAlias}):");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("ColumnExpression:");
            this.Visit(sqlColumnExpression.ColumnExpression);
            System.Diagnostics.Debug.Unindent();
            return sqlColumnExpression;
        }

        protected override SqlExpression VisitSqlDataSourceExpression(SqlDataSourceExpression sqlDataSourceExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlDataSourceExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("DataSourceAlias")).Value = sqlDataSourceExpression.DataSourceAlias.ToString();
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("DataSource");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlDataSourceExpression)} ({sqlDataSourceExpression.DataSourceAlias}):");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("DataSource:");
            this.Visit(sqlDataSourceExpression.QuerySource);
            System.Diagnostics.Debug.Unindent();
            return sqlDataSourceExpression;
        }

        protected override SqlExpression VisitSqlExistsExpression(SqlExistsExpression sqlExistsExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlExistsExpression));
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("SqlQuery");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlExistsExpression)}:");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("SqlQuery:");
            this.Visit(sqlExistsExpression.SqlQuery);
            System.Diagnostics.Debug.Unindent();
            return sqlExistsExpression;
        }

        //protected override SqlExpression VisitSqlFromSourceExpression(SqlFromSourceExpression sqlFromSourceExpression)
        //{
        //    var elem = XmlDoc.CreateElement(nameof(SqlFromSourceExpression));
        //    elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("DataSourceAlias")).Value = sqlFromSourceExpression.DataSourceAlias.ToString();
        //    currentNode.AppendChild(elem);
        //    currentNode = XmlDoc.CreateElement("DataSource");
        //    elem.AppendChild(currentNode);

        //    System.Diagnostics.Debug.WriteLine($"{nameof(SqlFromSourceExpression)} ({sqlFromSourceExpression.DataSourceAlias}):");
        //    System.Diagnostics.Debug.Indent();
        //    System.Diagnostics.Debug.WriteLine("DataSource:");
        //    this.Visit(sqlFromSourceExpression.DataSource);
        //    System.Diagnostics.Debug.Unindent();
        //    return sqlFromSourceExpression;
        //}

        protected override SqlExpression VisitSqlFunctionCallExpression(SqlFunctionCallExpression sqlFunctionCallExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlFunctionCallExpression));
            elem.Attributes.Append(XmlDoc.CreateAttribute("FunctionName")).Value = sqlFunctionCallExpression.FunctionName;
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlFunctionCallExpression)} ({sqlFunctionCallExpression.FunctionName}):");
            System.Diagnostics.Debug.Indent();
            var args = sqlFunctionCallExpression.Arguments?.ToArray() ?? Array.Empty<SqlExpression>();
            var subElem = XmlDoc.CreateElement("Arguments");
            elem.AppendChild(subElem);
            for (var i = 0; i < args.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"Argument");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"Argument {i}:");
                this.Visit(args[i]);
            }
            System.Diagnostics.Debug.Unindent();
            return sqlFunctionCallExpression;
        }

        protected override SqlExpression VisitSqlJoinExpression(SqlJoinExpression sqlJoinExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlJoinExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("JoinType")).Value = sqlJoinExpression.JoinType.ToString();
            currentNode.AppendChild(elem);
            
            System.Diagnostics.Debug.WriteLine($"{nameof(SqlJoinExpression)} ({sqlJoinExpression.JoinType}):");
            System.Diagnostics.Debug.Indent();

            currentNode = XmlDoc.CreateElement("JoinedSource");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("JoinedSource:");
            this.Visit(sqlJoinExpression.JoinedSource);

            currentNode = XmlDoc.CreateElement("JoinCondition");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("JoinCondition:");
            this.Visit(sqlJoinExpression.JoinCondition);

            System.Diagnostics.Debug.Unindent();

            return sqlJoinExpression;
        }

        protected override SqlExpression VisitSqlLiteralExpression(SqlLiteralExpression sqlLiteralExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlLiteralExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("LiteralValue")).Value = sqlLiteralExpression.LiteralValue?.ToString() ?? "null";
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlLiteralExpression)}: {sqlLiteralExpression.LiteralValue ?? "null"}");
            return sqlLiteralExpression;
        }

        protected override SqlExpression VisitSqlOrderByExpression(SqlOrderByExpression sqlOrderByExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlOrderByExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("Direction")).Value = sqlOrderByExpression.Ascending ? "asc" : "desc";
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("Expression");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlOrderByExpression)} ({(sqlOrderByExpression.Ascending ? "asc" : "desc")}):");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("Expression:");
            this.Visit(sqlOrderByExpression.Expression);
            System.Diagnostics.Debug.Unindent();
            return sqlOrderByExpression;
        }

        protected override SqlExpression VisitSqlQueryExpression(SqlQueryExpression sqlQueryExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlQueryExpression));
            elem.Attributes.Append(elem.OwnerDocument.CreateAttribute("IsCte")).Value = sqlQueryExpression.IsCte.ToString();
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("InitialDataSource");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlQueryExpression)} {(sqlQueryExpression.IsCte ? "(CTE)" : "")}:");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("InitialDataSource:");
            this.Visit(sqlQueryExpression.InitialDataSource);


            var cteDataSources = sqlQueryExpression.CteDataSources?.ToArray() ?? Array.Empty<SqlDataSourceExpression>();
            var subElem = XmlDoc.CreateElement("CteDataSources");
            elem.AppendChild(subElem);
            for (var i = 0; i < cteDataSources.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"CteDataSource");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"CteDataSource {i}:");
                this.Visit(cteDataSources[i]);
            }

            var joins = sqlQueryExpression.Joins?.ToArray() ?? Array.Empty<SqlJoinExpression>();
            subElem = XmlDoc.CreateElement("Joins");
            elem.AppendChild(subElem);
            for (var i = 0; i < joins.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"Join");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"Join {i}:");
                this.Visit(joins[i]);
            }

            System.Diagnostics.Debug.WriteLine("WhereClause:");
            var whereClause = sqlQueryExpression.WhereClause?.ToArray() ?? Array.Empty<FilterPredicate>();
            subElem = XmlDoc.CreateElement("WhereExpressions");
            elem.AppendChild(subElem);
            for (var i = 0; i < whereClause.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"WhereExpression");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("UseOrOperator")).Value = whereClause[i].UseOrOperator.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"Where {i}:");
                this.Visit(whereClause[i].Predicate);
            }
            
            currentNode = XmlDoc.CreateElement("GroupBy");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("GroupBy:");
            this.Visit(sqlQueryExpression.GroupBy);

            System.Diagnostics.Debug.WriteLine("HavingClause:");
            var havingClause = sqlQueryExpression.HavingClause?.ToArray() ?? Array.Empty<FilterPredicate>();
            subElem = XmlDoc.CreateElement("HavingExpressions");
            elem.AppendChild(subElem);
            for (var i = 0; i < havingClause.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"HavingExpression");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("UseOrOperator")).Value = havingClause[i].UseOrOperator.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"Having {i}:");
                this.Visit(havingClause[i].Predicate);
            }

            currentNode = XmlDoc.CreateElement("Projection");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("Projection:");
            this.Visit(sqlQueryExpression.Projection);

            var orderBy = sqlQueryExpression.OrderBy?.ToArray() ?? Array.Empty<SqlOrderByExpression>();
            subElem = XmlDoc.CreateElement("OrderByExpressions");
            elem.AppendChild(subElem);
            for (var i = 0; i < orderBy.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"OrderByExpression");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"OrderBy {i}:");
                this.Visit(orderBy[i]);
            }

            var unions = sqlQueryExpression.Unions?.ToArray() ?? Array.Empty<SqlUnionExpression>();
            subElem = XmlDoc.CreateElement("Unions");
            elem.AppendChild(subElem);
            for (var i = 0; i < unions.Length; i++)
            {
                currentNode = XmlDoc.CreateElement($"Union");
                currentNode.Attributes.Append(XmlDoc.CreateAttribute("Index")).Value = i.ToString();
                subElem.AppendChild(currentNode);

                System.Diagnostics.Debug.WriteLine($"Union {i} ({unions[i].NodeType}):");
                this.Visit(unions[i]);
            }

            currentNode = XmlDoc.CreateElement("Top");
            elem.AppendChild(currentNode);
            System.Diagnostics.Debug.WriteLine("Top:");
            this.Visit(sqlQueryExpression.Top);

            var rowOffsetElem = XmlDoc.CreateElement("RowOffset");
            rowOffsetElem.Attributes.Append(XmlDoc.CreateAttribute("Value")).Value = sqlQueryExpression.RowOffset?.ToString() ?? "null";
            elem.AppendChild(rowOffsetElem);

            System.Diagnostics.Debug.WriteLine($"RowOffset: {sqlQueryExpression.RowOffset}");
            var rowsPerPageElem = XmlDoc.CreateElement("RowsPerPage");
            rowsPerPageElem.Attributes.Append(XmlDoc.CreateAttribute("Value")).Value = sqlQueryExpression.RowsPerPage?.ToString() ?? "null";
            elem.AppendChild(rowsPerPageElem);

            System.Diagnostics.Debug.WriteLine($"RowsPerPage: {sqlQueryExpression.RowsPerPage}");
            System.Diagnostics.Debug.Unindent();

            return sqlQueryExpression;
        }

        protected override SqlExpression VisitSqlTableExpression(SqlTableExpression sqlTableExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlTableExpression));
            elem.Attributes.Append(XmlDoc.CreateAttribute("TableName")).Value = sqlTableExpression.TableName;
            currentNode.AppendChild(elem);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlTableExpression)} ({sqlTableExpression.TableName})");
            return sqlTableExpression;
        }

        protected override SqlExpression VisitSqlUnionExpression(SqlUnionExpression sqlUnionExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlUnionExpression));
            elem.Attributes.Append(XmlDoc.CreateAttribute("NodeType")).Value = sqlUnionExpression.NodeType.ToString();
            currentNode.AppendChild(elem);
            currentNode = XmlDoc.CreateElement("Query");
            elem.AppendChild(currentNode);

            System.Diagnostics.Debug.WriteLine($"{nameof(SqlUnionExpression)} ({sqlUnionExpression.NodeType}):");
            System.Diagnostics.Debug.Indent();
            System.Diagnostics.Debug.WriteLine("Query:");
            this.Visit(sqlUnionExpression.Query);
            System.Diagnostics.Debug.Unindent();
            return sqlUnionExpression;
        }

        protected override SqlExpression VisitCteReferenceExpression(SqlCteReferenceExpression sqlCteReferenceExpression)
        {
            var elem = XmlDoc.CreateElement(nameof(SqlCteReferenceExpression));
            elem.Attributes.Append(XmlDoc.CreateAttribute("CteAlias")).Value = sqlCteReferenceExpression.CteAlias.ToString();
            currentNode.AppendChild(elem);
            return sqlCteReferenceExpression;
        }
    }
}
