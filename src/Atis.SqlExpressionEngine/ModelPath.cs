using System;
using System.Collections.Generic;
using System.Linq;

namespace Atis.SqlExpressionEngine
{
    public readonly struct ModelPath
    {
        public ModelPath(string path)
        {
            this.Path = path;
            this.PathElements = path?.Split('.') ?? Array.Empty<string>();
        }

        public static ModelPath Empty { get; } = new ModelPath(path: null);

        public ModelPath(IEnumerable<string> pathElements)
        {
            if (pathElements != null)
                this.Path = string.Join(".", pathElements);
            else
                this.Path = null;
            this.PathElements = pathElements?.ToArray() ?? Array.Empty<string>();
        }

        public ModelPath(ModelPath modelPath)
        {
            this.Path = modelPath.Path;
            this.PathElements = modelPath.PathElements;
        }

        public string Path { get; }

        public string[] PathElements { get; }

        public bool IsEmpty => (this.PathElements?.Length ?? 0) == 0;


        public override bool Equals(object obj)
        {
            return obj is ModelPath modelPath &&
                   this.PathElements.SequenceEqual(modelPath.PathElements);
        }

        public override int GetHashCode()
        {
            // Use the HashCode class for consistent hash code calculation  
            var hash = new HashCode();
            foreach (var element in this.PathElements)
            {
                hash.Add(element);
            }
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return this.IsEmpty ? "(empty path)" : this.Path;
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
                return !this.IsEmpty;
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

        public bool StartsWith(ModelPath path)
        {
            return this.StartsWith(path.PathElements);
        }


        public ModelPath Append(params string[] otherPathElements)
        {
            if (otherPathElements is null || otherPathElements.Length == 0)
                return this;
            if (this.IsEmpty)
                return new ModelPath(otherPathElements);
            return new ModelPath(this.PathElements.Concat(otherPathElements));
        }

        public ModelPath Append(ModelPath otherModelPath)
        {
            return this.Append(otherModelPath.PathElements);
        }

        public string GetLastElementRequired()
        {
            return this.PathElements.LastOrDefault()
                    ??
                     throw new InvalidOperationException("Path is empty.");
        }

        public ModelPath RemoveElementsFromLeft(int numberEntriesToRemove)
        {
            if (this.IsEmpty)
                return this;
            if (numberEntriesToRemove <= 0)
                return this;
            if (numberEntriesToRemove >= this.PathElements.Length)
                return Empty;
            var newPathElements = this.PathElements.Skip(numberEntriesToRemove);
            return new ModelPath(newPathElements);
        }

        public ModelPath RemoveFromLeft(ModelPath startPathToRemove)
        {
            if (this.IsEmpty)
                return this;
            if (startPathToRemove.IsEmpty)
                return this;
            if (!this.StartsWith(startPathToRemove))
                throw new InvalidOperationException($"Cannot remove {startPathToRemove} from {this}.");
            var newPathElements = this.PathElements.Skip(startPathToRemove.PathElements.Length);
            return new ModelPath(newPathElements);
        }
    }
}
