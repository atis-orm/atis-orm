using Atis.SqlExpressionEngine.SqlExpressions;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class ModelPathTests
    {
        [TestMethod]
        public void EndsWith_should_return_True_when_path_ends_with_given_elements()
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
        public void EndsWith_should_return_False_when_path_does_not_end_with_given_elements()
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
        public void EndsWith_should_return_False_when_path_is_empty()
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
        public void EndsWith_should_return_True_when_given_elements_are_empty()
        {
            // Arrange
            var modelPath = new ModelPath("a.b.c.d");
            var pathElements = new string[] { };

            // Act
            var result = modelPath.EndsWith(pathElements);

            // Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void ModelPath_equals_test()
        {
            var m1 = new ModelPath("a");
            var m2 = new ModelPath("a");
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("a");
            m2 = new ModelPath("b");
            if (m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath(path: null);
            m2 = new ModelPath(path: null);
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("a.b");
            m2 = new ModelPath("a.b");
            if (!m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");

            m1 = new ModelPath("b.a");
            m2 = new ModelPath("a.b");
            if (m1.Equals(m2))
                Assert.Fail("ModelPath Equals Test Failed");
        }


    }
}
