using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.SqlExpressionEngine.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when the maximum number of iterations is exceeded during post processing.
    /// </summary>
    public class PostprocessingThresholdExceededException : Exception
    {
        /// <summary>
        /// Creates a new instance of the PostprocessingThresholdExceededException class.
        /// </summary>
        /// <param name="maxIterations">Number of maximum iterations allowed.</param>
        public PostprocessingThresholdExceededException(int maxIterations)
        : base($"Post processing of SqlExpression exceeded the maximum number of iterations ({maxIterations}).")
        {
        }
    }
}
