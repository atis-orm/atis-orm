# Expression Conversion in Atis ORM

This guide explains how expression conversion works in the Atis ORM, particularly focusing on the converter mechanism and an example use case with string comparison.

---

## 🧩 How Conversion Works in General

In Atis ORM, every expression conversion is handled using two components:

### 1. **Factory Class**
A factory class (e.g., `StringCompareToConverterFactory`) is responsible for:
- Inspecting the LINQ expression (like `BinaryExpression`, `MethodCallExpression`, etc.)
- Determining whether the expression **matches a specific pattern**
- Creating and returning an instance of a specialized **converter class** for that expression

### 2. **Converter Class**
The converter class (e.g., `StringCompareToConverter`) is responsible for:
- Performing the **actual conversion** to a `SqlExpression`
- Maintaining **state** during the recursive conversion of sub-expressions
- Providing child converters if needed (by overriding `TryCreateChildConverter`)


> 📌 This separation allows each converter to manage its own lifecycle and internal state, enabling complex expression rewriting or context-sensitive logic.

---

## 🔍 Example: `StringCompareToConverter`

### 🎯 Goal

Convert LINQ expressions like:

```csharp
string.Compare(a, b) == 0
string.Compare(a, b) > 0
a.CompareTo(b) <= 1
```

to SQL expressions like:

```sql
a = b
a > b
a < b
```

### 🧠 How It Works

1. **Factory Matching**
   - The `StringCompareToConverterFactory` checks if:
     - The expression is a `BinaryExpression`
     - The left side is a method call: `string.Compare(...)` or `.CompareTo(...)`
     - The right side is a constant `0` or `1`

```csharp
public override bool TryCreate(Expression expression, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> converter)
{
    if (expression is BinaryExpression binaryExpression &&
        (binaryExpression.NodeType == ExpressionType.GreaterThan ||
         binaryExpression.NodeType == ExpressionType.GreaterThanOrEqual ||
         binaryExpression.NodeType == ExpressionType.LessThan ||
         binaryExpression.NodeType == ExpressionType.LessThanOrEqual ||
         binaryExpression.NodeType == ExpressionType.Equal ||
         binaryExpression.NodeType == ExpressionType.NotEqual) &&
        binaryExpression.Left is MethodCallExpression methodCallExpression &&
        (methodCallExpression.Method.Name == nameof(string.CompareTo) ||
         methodCallExpression.Method.Name == nameof(string.Compare)) &&
        methodCallExpression.Method.DeclaringType == typeof(string) &&
        binaryExpression.Right is ConstantExpression constantExpression &&
        (constantExpression.Value?.Equals(0) == true || constantExpression.Value?.Equals(1) == true))
    {
        converter = new StringCompareToConverter(Context, binaryExpression, converterStack);
        return true;
    }

    converter = null;
    return false;
}
```

2. **Custom Converter Creation**
   - The `StringCompareToConverter` intercepts the left-side method call by overriding `TryCreateChildConverter`.

```csharp
public override bool TryCreateChildConverter(Expression childNode, ExpressionConverterBase<Expression, SqlExpression>[] converterStack, out ExpressionConverterBase<Expression, SqlExpression> childConverter)
{
    if (childNode == this.Expression.Left && childNode is MethodCallExpression stringCompareMethodCall)
    {
        childConverter = new StringCompareMethodConverter(Context, stringCompareMethodCall, converterStack);
        return true;
    }
    return base.TryCreateChildConverter(childNode, converterStack, out childConverter);
}
```

3. **Child Converter Output**
   - `StringCompareMethodConverter` simply returns a `SqlCollectionExpression` with two values: the left and right string expressions.

```csharp
public override SqlExpression Convert(SqlExpression[] convertedChildren)
{
    if (convertedChildren.Length < 2)
        throw new InvalidOperationException("String.CompareTo / String.Compare requires at least 2 arguments.");

    return new SqlCollectionExpression(convertedChildren.Take(2));
}
```

4. **Final SQL Generation**
   - Based on the binary operator (`==`, `!=`, `<`, `>=`, etc.) and constant (`0` or `1`), the converter selects the corresponding `SqlExpressionType`.

```csharp
SqlExpressionType binaryNodeType;
switch (this.Expression.NodeType)
{
    case ExpressionType.GreaterThan:
        binaryNodeType = SqlExpressionType.GreaterThan;
        break;
    case ExpressionType.GreaterThanOrEqual:
        binaryNodeType = constantValue == 1 ? SqlExpressionType.GreaterThan : SqlExpressionType.GreaterThanOrEqual;
        break;
    case ExpressionType.LessThan:
        binaryNodeType = SqlExpressionType.LessThan;
        break;
    case ExpressionType.LessThanOrEqual:
        binaryNodeType = constantValue == 1 ? SqlExpressionType.LessThan : SqlExpressionType.LessThanOrEqual;
        break;
    case ExpressionType.Equal:
        binaryNodeType = constantValue == 1 ? SqlExpressionType.GreaterThan : SqlExpressionType.Equal;
        break;
    case ExpressionType.NotEqual:
        binaryNodeType = constantValue == 1 ? SqlExpressionType.LessThanOrEqual : SqlExpressionType.NotEqual;
        break;
    default:
        throw new InvalidOperationException($"Unsupported binary expression type: {this.Expression.NodeType}.");
}
```

---

## ✅ Why Not Use the General `MethodCallConverter`?

- The general converter would wrap the `string.Compare` method call in a `SqlFunctionCallExpression`, which is not useful here.
- The parent converter would need to inspect and unpack that function call, introducing tight coupling and unpredictability.
- By using a dedicated child converter, the parent retains **full control**, ensures a **predictable structure**, and avoids unnecessary complexity.

---

## ✅ Summary

[View `StringCompareToConverter.cs`](https://github.com/sallushan/atis-orm/blob/main/src/Atis.SqlExpressionEngine/ExpressionConverters/StringCompareToConverter.cs)


| Component               | Responsibility                               |
|------------------------|-----------------------------------------------|
| Factory                | Detects match, creates the correct converter  |
| Converter              | Performs conversion, manages child handling   |
| Child Converter (custom) | Returns a structure tailored for parent use |

This design ensures Atis ORM handles complex expression translations with clarity, flexibility, and maintainability.
