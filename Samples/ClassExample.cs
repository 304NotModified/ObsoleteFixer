using System;
using System.Collections.Generic;
using System.Text;

namespace Samples
{
    [Obsolete("Replace with `OtherClassExample`")]
    class ClassExample
    {
        public void MyMethod(string x, object y)
        {

        }

        [Obsolete("Replace with `OtherClassExample.MyNewMethod(x,y,2.ToString())`")]
        public void MyMethod2(string x, object y)
        {

        }
    }

    class OtherClassExample
    {
        public void MyMethod(string x, object y)
        {

        }

        public void MyNewMethod(object y, string x, string y2)
        {

        }
    }

    class ClassInvoker
    {
        public void Invoke()
        {
            new ClassExample{}.MyMethod("text", 2);

            new ClassExample().MyMethod2("text", 2);
        }
    }

}
