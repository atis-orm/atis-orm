using Atis.LinqToSql.SqlExpressions;

namespace Atis.LinqToSql.UnitTest
{
    [TestClass]
    public class ModelPathTests
    {
        [TestMethod]
        public void EndsWith_ShouldReturnTrue_WhenPathEndsWithGivenElements()
        {
            // Arrange
            var modelPath = new ModelPath("a.b.c.d");
            var pathElements = new[] { "c", "d" };

            // Act
            var result = modelPath.EndsWith(pathElements);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void EndsWith_ShouldReturnFalse_WhenPathDoesNotEndWithGivenElements()
        {
            // Arrange
            var modelPath = new ModelPath("a.b.c.d");
            var pathElements = new[] { "b", "c" };

            // Act
            var result = modelPath.EndsWith(pathElements);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void EndsWith_ShouldReturnFalse_WhenPathIsEmpty()
        {
            // Arrange
            var modelPath = ModelPath.Empty;
            var pathElements = new[] { "a" };

            // Act
            var result = modelPath.EndsWith(pathElements);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void EndsWith_ShouldReturnTrue_WhenGivenElementsAreEmpty()
        {
            // Arrange
            var modelPath = new ModelPath("a.b.c.d");
            var pathElements = new string[] { };

            // Act
            var result = modelPath.EndsWith(pathElements);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
