using System;
using System.Collections.Generic;
using System.Text;

namespace Atis.Expressions
{
    public class PreprocessingThresholdExceededException : Exception
    {
        public PreprocessingThresholdExceededException(int maxIterations)
        : base($"Preprocessing exceeded the maximum number of iterations ({maxIterations}).")
        {
        }
    }
}
