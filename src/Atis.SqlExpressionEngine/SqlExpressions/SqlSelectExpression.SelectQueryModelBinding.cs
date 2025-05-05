using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atis.SqlExpressionEngine.SqlExpressions
{
    public partial class SqlSelectExpression
    {
        private class SqlQueryModelBinding
		{
            private readonly List<SqlExpressionBinding> bindings = new List<SqlExpressionBinding>();

			public void AddBinding(SqlExpression sqlExpression, ModelPath modelPath)
				=> this.AddBinding(sqlExpression, modelPath, false);

            public void AddBinding(SqlExpression sqlExpression, ModelPath modelPath, bool nonProjectable)
			{
				if (sqlExpression is null)
					throw new ArgumentNullException(nameof(sqlExpression));
				if (this.bindings.Any(x => x.ModelPath.Equals(modelPath)))
					throw new ArgumentException($"Binding with model path '{modelPath}' already exists.", nameof(modelPath));
				var binding = nonProjectable
                    ? new NonProjectableBinding(sqlExpression, modelPath)
                    : new SqlExpressionBinding(sqlExpression, modelPath);
                this.bindings.Add(binding);
			}

			public void AddBindings(SqlExpressionBinding[] bindings)
			{
				if (!(bindings.Length > 0))
					throw new ArgumentNullException(nameof(bindings));

				foreach (var binding in bindings)
				{
					this.AddBinding(binding.SqlExpression, binding.ModelPath, nonProjectable: binding is NonProjectableBinding);
				}
			}

			public void UpdatePath(SqlExpressionBinding binding, ModelPath newModelPath)
			{
				var bindingIndex = this.bindings.IndexOf(binding);
				if (bindingIndex < 0)
					throw new ArgumentException($"Binding with model path '{binding.ModelPath}' does not exist.", nameof(binding));
				SqlExpressionBinding newBinding;
				if (binding is NonProjectableBinding)
					newBinding = new NonProjectableBinding(binding.SqlExpression, newModelPath);
				else
					newBinding = new SqlExpressionBinding(binding.SqlExpression, newModelPath);
				this.bindings[bindingIndex] = newBinding;
			}

			public void MarkBindingAsNonProjectable(ModelPath modelPath)
			{
				var binding = this.bindings.Where(x => x.ModelPath.Equals(modelPath)).FirstOrDefault()
								??
								throw new ArgumentException($"Binding with model path '{modelPath}' does not exist.", nameof(modelPath));
                var bindingIndex = this.bindings.IndexOf(binding);
				if (bindingIndex < 0)
					throw new ArgumentException($"Binding with model path '{binding.ModelPath}' does not exist.", nameof(binding));
                this.bindings[bindingIndex] = new NonProjectableBinding(binding.SqlExpression, binding.ModelPath);
            }

			public SqlExpressionBinding[] GetProjectableBindings() => this.bindings.Where(x => !(x is NonProjectableBinding)).ToArray();

            public SqlExpressionBinding[] GetFilteredByExpression(Func<SqlExpression, bool> filter)
            {
                return this.bindings.Where(x => filter(x.SqlExpression)).ToArray();
            }

			public SqlExpressionBinding[] CreateCopy() => this.bindings.ToArray();

			public SqlExpressionBinding[] PrependPath(ModelPath modelPathToPrepend) 
				=> this.bindings.Select(x=>new SqlExpressionBinding(x.SqlExpression, modelPathToPrepend.Append(x.ModelPath))).ToArray();

            public SqlExpressionBinding[] ResolvePartial(ModelPath path)
			{
				return this.bindings.Where(x => x.ModelPath.StartsWith(path)).ToArray();
			}

			public bool TryResolveExact(ModelPath path, out SqlExpression resolvedSqlExpression)
			{
				var entry = this.bindings.Where(x => x.ModelPath.Equals(path)).FirstOrDefault();
				if (entry != null)
				{
					resolvedSqlExpression = entry.SqlExpression;
					return true;
				}
				resolvedSqlExpression = null;
				return false;
			}

			public void Reset()
            {
				this.bindings.Clear();
			}

            public void Remove(SqlExpressionBinding binding)
            {
				this.bindings.Remove(binding);
            }

            public SqlExpressionBinding[] GetNonProjectableBindings() => this.bindings.Where(x => x is NonProjectableBinding).ToArray();

			public void UpdateSqlExpression(SqlExpressionBinding binding, SqlExpression sqlExpression)
			{
				if (binding is null)
					throw new ArgumentNullException(nameof(binding));
				if (sqlExpression is null)
					throw new ArgumentNullException(nameof(sqlExpression));
				var bindingIndex = this.bindings.IndexOf(binding);
				if (bindingIndex < 0)
					throw new InvalidOperationException($"Binding was not found");
                if (binding is NonProjectableBinding)
					this.bindings[bindingIndex] = new NonProjectableBinding(sqlExpression, binding.ModelPath);
				else
					this.bindings[bindingIndex] = new SqlExpressionBinding(sqlExpression, binding.ModelPath);
			}

			public void RemoveByPath(ModelPath modelPath)
			{
				var binding = this.bindings.Where(x => x.ModelPath.Equals(modelPath)).FirstOrDefault()
								??
								throw new ArgumentException($"Binding with model path '{modelPath}' does not exist.", nameof(modelPath));
				this.bindings.Remove(binding);
			}
        }
	}
}
