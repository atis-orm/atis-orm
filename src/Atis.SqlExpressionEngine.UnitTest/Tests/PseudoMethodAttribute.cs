// Bismilla-hir-Rahman-nir-Rahim
// In the name of Allah the most beneficial and merciful.
/*
 * Rules:
 *  1) We don't allow the translation system to perform nested translation within the converter. For example,
 *  ﻿   in Binary Expression converter plugin we will receive left and right node already converted, Binary Expression
 *  ﻿   will NOT be accessing main converter system to do any further conversion.
 *  ﻿   Another scenario is that, let say we have a variable in the expression of type of IQueryable<T>, like
 *  ﻿       var q1 = QueryExtensions.DataSet<Student>().Where(x => x.IsDisabled == false);
 *  ﻿       var q2 = QueryExtensions.DataSet<StudentGrade>().Where(x => q1.Any(y => y.StudentId == x.StudentId));
 *  ﻿   'q1' is going to be received by ConstantExpressionConverter, and constant expression converter
 *  ﻿   simply creates a parameter expression. But in this case, we might think that why not create a separate converter
 *     which will go in Expression property of q1 and can perform further conversion. But as this rule states
 *  ﻿   that this should NOT be done, because converter plugin has no access to translation system.
 *  ﻿   
 */


namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    public class PseudoMethodAttribute : Attribute
    {
        public PseudoMethodAttribute(string expressionProperty)
        {
            this.ExpressionProperty = expressionProperty;
        }

        public string ExpressionProperty { get; }

    }
}