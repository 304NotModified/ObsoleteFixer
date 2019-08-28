using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObsoleteFixer.Test.Verifiers;

namespace ObsoleteFixer.Test
{
    [TestClass]
    public class ObsoleteFixerCodeFixProviderTests : CodeFixVerifier
    {

        [TestMethod]
        public void FixPropertyAndTransformParametersTest()
        {
            var old = @"
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

            [Obsolete(""Replace with `MyNewMethod`"")]
            public void MyOldMethod(string x, object y)
            {

            }

            public void MyNewMethod(string x2, object y2)
            {

            }
        }


    }";

            var expected = @"
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
                myClass.MyNewMethod(""text"", 2);
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewMethod`"")]
            public void MyOldMethod(string x, object y)
            {

            }

            public void MyNewMethod(string x2, object y2)
            {

            }
        }


    }";

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void FixMethodCallTest()
        {
            var old = @"
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

            var expected = @"
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
                myClass.MyNewMethod(2, ""text"", ""text2"");
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

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void FixStaticMethodInCurrentClassTest()
        {
            var old = @"
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
                MyClass.MyOldMethod(""text"", 2);
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewMethod(y,x,\""text2\"")`"")]
            public static void MyOldMethod(string x, object y)
            {

            }

            public static void MyNewMethod(object y, string x, string y2)
            {

            }
        }
    }";

            var expected = @"
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
                MyClass.MyNewMethod(2, ""text"", ""text2"");
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewMethod(y,x,\""text2\"")`"")]
            public static void MyOldMethod(string x, object y)
            {

            }

            public static void MyNewMethod(object y, string x, string y2)
            {

            }
        }
    }";

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void FixStaticMethodInOtherClassTest()
        {
            var old = @"
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
                MyClass.MyOldMethod(""text"", 2);
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewClass.MyNewMethod(y,x,\""text2\"")`"")]
            public static void MyOldMethod(string x, object y)
            {

            }
        }
        class MyNewClass
        {

            public static void MyNewMethod(object y, string x, string y2)
            {

            }
        }
    }";

            var expected = @"
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
                MyNewClass.MyNewMethod(2, ""text"", ""text2"");
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewClass.MyNewMethod(y,x,\""text2\"")`"")]
            public static void MyOldMethod(string x, object y)
            {

            }
        }
        class MyNewClass
        {

            public static void MyNewMethod(object y, string x, string y2)
            {

            }
        }
    }";


            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void FixMultipleMethodCallsTest()
        {
            var old = @"
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

            public void CallMethod2()
            {
                var myClass = new MyClass();
                myClass.MyOldMethod(""text3"", 3);
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

            var expected = @"
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
                myClass.MyNewMethod(2, ""text"", ""text2"");
            }

            public void CallMethod2()
            {
                var myClass = new MyClass();
                myClass.MyNewMethod(3, ""text3"", ""text2"");
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

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void PropertySetterTest1()
        {
            var old = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        class Sample
        {
            public void Invoke()
            {
                var myClass = new MyClass();
                myClass.MyOldProperty = 2;
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewProperty`"")]
            public int MyOldProperty {get;set;}

            public int MyNewProperty {get;set;}
        }
    }";

            var expected = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        class Sample
        {
            public void Invoke()
            {
                var myClass = new MyClass();
                myClass.MyNewProperty = 2;
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewProperty`"")]
            public int MyOldProperty {get;set;}

            public int MyNewProperty {get;set;}
        }
    }";

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void PropertyGetterTest1()
        {
            var old = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        class Sample
        {
            public void Invoke()
            {
                var myClass = new MyClass();
                var val = myClass.MyOldProperty;
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewProperty`"")]
            public int MyOldProperty {get;set;}

            public int MyNewProperty {get;set;}
        }
    }";

            var expected = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        class Sample
        {
            public void Invoke()
            {
                var myClass = new MyClass();
                var val = myClass.MyNewProperty;
            }
        }

        class MyClass
        {

            [Obsolete(""Replace with `MyNewProperty`"")]
            public int MyOldProperty {get;set;}

            public int MyNewProperty {get;set;}
        }
    }";

            VerifyCSharpFix(old, expected);

        }

        [TestMethod]
        public void ClassTest1()
        {
            var old = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        [Obsolete(""Replace with `MyOtherClass`"")]
        class MyClass
        {
            public void MyMethod(string x, object y)
            {

            }
        }

        class MyOtherClass
        {
            public void MyMethod(string x, object y)
            {

            }
        }

        class ClassInvoker
        {
            public void Invoke()
            {
                new MyClass().MyMethod(""text"", 2);
            }
        }
    }";

            var expected = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        [Obsolete(""Replace with `MyOtherClass`"")]
        class MyClass
        {
            public void MyMethod(string x, object y)
            {

            }
        }

        class MyOtherClass
        {
            public void MyMethod(string x, object y)
            {

            }
        }

        class ClassInvoker
        {
            public void Invoke()
            {
                new MyOtherClass().MyMethod(""text"", 2);
            }
        }
    }";

            VerifyCSharpFix(old, expected);

        }
        
        [TestMethod]
        public void MethodPrecedesClassFixTest()
        {
            var old = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        [Obsolete(""Replace with `MyOtherClass`"")]
        class MyClass
        {
            public void MyMethod(string x, object y)
            {

            }

            [Obsolete(""Replace with `MyOtherClass.MyNewMethod(y, x, 2.ToString())`"")]
            public void MyMethod2(string x, object y)
            {

            }
        }

        class MyOtherClass
        {
            public void MyNewMethod(object y, string x, string y2)
            {

            }
        }

        class ClassInvoker
        {
            public void Invoke()
            {
                new MyClass().MyMethod2(""text"", 2);
            }
        }
    }";

            var expected = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace MyApplication
    {
        [Obsolete(""Replace with `MyOtherClass`"")]
        class MyClass
        {
            public void MyMethod(string x, object y)
            {

            }

            [Obsolete(""Replace with `MyOtherClass.MyNewMethod(y, x, 2.ToString())`"")]
            public void MyMethod2(string x, object y)
            {

            }
        }

        class MyOtherClass
        {
            public void MyNewMethod(object y, string x, string y2)
            {

            }
        }

        class ClassInvoker
        {
            public void Invoke()
            {
                new MyOtherClass().MyNewMethod(2, ""text"", 2.ToString());
            }
        }
    }";

            VerifyCSharpFix(old, expected);

        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ObsoleteFixerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ObsoleteFixerAnalyzer();
        }
    }
}
