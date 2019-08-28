using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObsoleteFixer.Test.Verifiers;
using DiagnosticVerifier = ObsoleteFixer.Test.Verifiers.DiagnosticVerifier;

namespace ObsoleteFixer.Test
{
    [TestClass]
    public class ObsoleteFixerAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        public void EmptyDiagnosticTest()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DiagnosticTest1()
        {
            var code = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        class Caller
        {
            public void CallMethod()
            {
                var myClass = new MyClass();
                myClass.MyOldMethod(""text"", 2);
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewMethod(y,x,\""text2\"")`"")]
            public void MyOldMethod(string x, object y)
            {

            }

            public void MyNewMethod(object y, string x, string y2)
            {

            }
        }


    }";
            var expected = new DiagnosticResult
            {
                Id = "ObsoleteFixer",
                Message = "Method is obsolete 'MyOldMethod' and should be replaced with 'MyNewMethod(y,x,\"text2\")'",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 16, 17) //column of whole call
                    }
            }; //todo fix duplicates

            VerifyCSharpDiagnostic(code, expected, expected); //todo fix not duplicate
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ObsoleteFixerAnalyzer();
        }
    }
}
