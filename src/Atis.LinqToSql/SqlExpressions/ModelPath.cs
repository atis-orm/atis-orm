using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.LinqToSql.SqlExpressions
{
    /// <summary>
    ///     <para>
    ///         Represents a model path which is a dot-separated string path.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ModelPath"/> is crucial in the LINQ Expression to SqlExpression normalization process, ensuring that model paths are 
    ///         consistently tracked and mapped between LINQ expressions and SQL expressions. It helps in correctly 
    ///         generating column aliases and mapping database results back to object graphs.
    ///     </para>
    ///     <para>
    ///         When a LINQ expression is transformed into a SQL query, the model path helps identify how members are accessed.
    ///         The <see cref="ExpressionConverters.MemberExpressionConverter"/> class, for example, utilizes <see cref="ModelPath"/> to determine
    ///         the correct SQL representation of member expressions. It checks whether a given model path corresponds to 
    ///         a SQL column, a data source, or a computed projection.
    ///     </para>
    /// </remarks>
    public readonly struct ModelPath
    {
        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ModelPath"/> struct with the specified path.
        ///     </para>
        /// </summary>
        /// <param name="path">The dot-separated string path.</param>
        public ModelPath(string path)
        {
            this.Path = path;
            this.PathElements = path?.Split('.') ?? Array.Empty<string>();
        }

        /// <summary>
        ///     <para>
        ///         Gets an empty <see cref="ModelPath"/> instance.
        ///     </para>
        /// </summary>
        public static ModelPath Empty { get; } = new ModelPath(path: null);

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ModelPath"/> struct with the specified path elements.
        ///     </para>
        /// </summary>
        /// <param name="pathElements">The path elements as an enumerable of strings.</param>
        public ModelPath(IEnumerable<string> pathElements)
        {
            if (pathElements != null)
                this.Path = string.Join(".", pathElements);
            else
                this.Path = null;
            this.PathElements = pathElements?.ToArray() ?? Array.Empty<string>();
        }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref="ModelPath"/> struct by copying another instance.
        ///     </para>
        /// </summary>
        /// <param name="modelPath">The <see cref="ModelPath"/> instance to copy.</param>
        public ModelPath(ModelPath modelPath)
        {
            this.Path = modelPath.Path;
            this.PathElements = modelPath.PathElements;
        }

        /// <summary>
        ///     <para>
        ///         Gets the dot-separated string path.
        ///     </para>
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     <para>
        ///         Gets the path elements as an array of strings.
        ///     </para>
        /// </summary>
        public string[] PathElements { get; }

        /// <summary>
        ///     <para>
        ///         Gets a value indicating whether the path is empty.
        ///     </para>
        /// </summary>
        public bool IsEmpty => (this.PathElements?.Length ?? 0) == 0;

        /// <summary>
        ///     <para>
        ///         Determines whether the current path ends with the specified path elements.
        ///     </para>
        /// </summary>
        /// <param name="pathElements">The path elements as an array of strings.</param>
        /// <returns></returns>
        public bool EndsWith(string[] pathElements)
        {
            if (pathElements is null || pathElements.Length == 0)
            {
                return this.IsEmpty;
            }
            // if we are here it means pathElements is neither null nor empty
            // that's why we are saying if this.IsEmpty then return false
            if (this.IsEmpty)
                return false;
            var myPathElements = this.PathElements;
            var pathLength = pathElements.Length;
            var myPathLength = myPathElements.Length;
            for (int i = 0; i < pathLength; i++)
            {
                if (i >= myPathLength || pathElements[pathLength - i - 1] != myPathElements[myPathLength - i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     <para>
        ///         Determines whether the current path starts with the specified starting path elements.
        ///     </para>
        /// </summary>
        /// <param name="pathElements">The path elements as an array of strings.</param>
        /// <returns>True if the current path starts with the specified starting path elements; otherwise, false.</returns>
        public bool StartsWith(string[] pathElements)
        {
            if (pathElements is null || pathElements.Length == 0)
            {
                return this.IsEmpty;
            }
            // if we are here it means pathElements is neither null nor empty
            // that's why we are saying if this.IsEmpty then return false
            if (this.IsEmpty)
                return false;
            var myPathElements = this.PathElements;
            for (int i = 0; i < pathElements.Length; i++)
            {
                if (i >= myPathElements.Length || pathElements[i] != myPathElements[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     <para>
        ///         Replaces the last path entry with the specified new path entry.
        ///     </para>
        /// </summary>
        /// <param name="newPathEntry">The new path entry.</param>
        /// <returns>A new <see cref="ModelPath"/> instance with the last path entry replaced.</returns>
        public ModelPath ReplaceLastPathEntry(string newPathEntry)
        {
            if (string.IsNullOrEmpty(newPathEntry))
                return this;
            var myPathElements = this.IsEmpty ? new[] { "temp" } : this.PathElements;
            // removing the last entry and adding the new one
            var newArray = myPathElements.Take(myPathElements.Length - 1).Concat(new[] { newPathEntry });
            return new ModelPath(newArray);
        }

        /// <summary>
        ///     <para>
        ///         Appends the specified path to the current path.
        ///     </para>
        /// </summary>
        /// <param name="path">The path to append as a dot-separated string.</param>
        /// <returns>A new <see cref="ModelPath"/> instance with the specified path appended.</returns>
        public ModelPath Append(string path)
        {
            if (string.IsNullOrEmpty(path))
                return this;
            if (this.IsEmpty)
                return new ModelPath(path);
            var otherPathElements = path.Split('.');
            return new ModelPath(this.PathElements.Concat(otherPathElements));
        }

        /// <summary>
        ///     <para>
        ///         Appends the specified path elements to the current path.
        ///     </para>
        /// </summary>
        /// <param name="otherPathElements">The path elements to append as an array of strings.</param>
        /// <returns>A new <see cref="ModelPath"/> instance with the specified path elements appended.</returns>
        public ModelPath Append(string[] otherPathElements)
        {
            if (otherPathElements is null || otherPathElements.Length == 0)
                return this;
            if (this.IsEmpty)
                return new ModelPath(otherPathElements);
            return new ModelPath(this.PathElements.Concat(otherPathElements));
        }

        /// <summary>
        ///     <para>
        ///         Appends the specified <see cref="ModelPath"/> to the current path.
        ///     </para>
        /// </summary>
        /// <param name="otherModelPath">The <see cref="ModelPath"/> to append.</param>
        /// <returns>A new <see cref="ModelPath"/> instance with the specified <see cref="ModelPath"/> appended.</returns>
        public ModelPath Append(ModelPath otherModelPath)
        {
            return this.Append(otherModelPath.PathElements);
        }

        /// <summary>
        ///     <para>
        ///         Matches the given <paramref name="prefixPathElements"/> with <see cref="PathElements"/> of this instance,
        ///         removes the prefix path from the current path and returns the remaining path as new instance of <see cref="ModelPath"/> struct.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is used in <see cref="ExpressionConverters.MemberExpressionConverter"/>. When selecting an element
        ///         this method removes the start part of the path so that later property accessing is done with new member path.
        ///     </para>
        ///     <code>
        ///         1 var q = From(() => new { t1 = Table&lt;Table1&gt;(), t2 = Table&lt;Table2&gt;() })
        ///         2             .LeftJoin(x => x.t2, x => x.t1.PK == x.t2.FK)
        ///         3             .LeftJoin(Table&lt;Table3&gt;(), (oldType, joinedType) => new { o = oldType, t3 = joinedType }, x => x.o.t1.FK == x.t3.PK)
        ///         4             .Select(x => new { y = x.o });
        ///         5             .Select(x => new { x.y.t1.Field1, x.y.t1.Field2 });
        ///                     
        ///     </code>
        ///     <para>
        ///         Before line number 4, the path to access Data Sources 't1' and 't2' was "o". After line number 4, the path to access these data sources will be "y".
        ///         When <see cref="ExpressionConverters.MemberExpressionConverter"/> converts 'x.o', it finds that this expression is basically representing
        ///         the collection of <see cref="SqlExpressions.SqlDataSourceExpression"/> i.e., 't1' and 't2'. Both of these data sources have model path
        ///         'o.t1' and 'o.t2' respectively. But since in this 'Select', we are selecting 'x.o' directly, therefore, in preceding calls, the Data Sources
        ///         will not be accessed using 'o', that's where the converter will call this method to remove the prefix 'o' from the path of these
        ///         data sources.
        ///     </para>
        ///     <para>
        ///         Similarly, if the selected member expression is being translated to a collection of <see cref="SqlExpressions.SqlColumnExpression"/>, same
        ///         logic is applied and the prefix path is removed from the model path of the column expression.
        ///     </para>
        /// </remarks>
        /// <param name="prefixPathElements">Prefix path elements to match and remove.</param>
        /// <returns>New <see cref="ModelPath"/> instance with matching prefix part removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ModelPath RemovePrefixPath(string[] prefixPathElements)
        {
            if (prefixPathElements is null || prefixPathElements.Length == 0)
                throw new ArgumentNullException(nameof(prefixPathElements));
            if (this.IsEmpty)
                return new ModelPath(path: null);
            var thisMapAsArray = this.PathElements;
            int i;
            for (i = 0; i < prefixPathElements.Length; i++)
            {
                if (i >= thisMapAsArray.Length || thisMapAsArray[i] != prefixPathElements[i])
                    break;
            }
            // at this point i will be the point where they don't match
            // we are skipping "i" because we need to pick from "i"
            // e.g. i = 0 didn't match, so it means 1st element is not matching
            // so we need to pick from first index, which means skip = 0.
            return new ModelPath(thisMapAsArray.Skip(i));
        }

        /// <summary>
        ///     <para>
        ///         Gets the last element of the path.
        ///     </para>
        /// </summary>
        /// <returns>The last element of the path as a string.</returns>
        public string GetLastElement()
        {
            return this.PathElements.Last();
        }

        /// <summary>
        ///     <para>
        ///         Determines whether the specified object is equal to the current <see cref="ModelPath"/> instance.
        ///     </para>
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ModelPath modelPath &&
                   this.PathElements.SequenceEqual(modelPath.PathElements);
        }

        /// <summary>
        ///     <para>
        ///         Returns a hash code for the current <see cref="ModelPath"/> instance.
        ///     </para>
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            foreach (var element in this.PathElements)
            {
                hashCode.Add(element);
            }
            return hashCode.GetHashCode();
        }

        /// <summary>
        ///     <para>
        ///         Returns a string that represents the current <see cref="ModelPath"/> instance.
        ///     </para>
        /// </summary>
        /// <returns>A string that represents the current instance.</returns>
        public override string ToString()
        {
            return this.IsEmpty ? "(empty path)" : this.Path;
        }
    }
}
