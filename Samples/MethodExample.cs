using System;

namespace Samples
{
    public class MethodExample
    {
        [Obsolete("Replace with `MyNewMethod(y, x, \"text2\")`")]
        public void MyOldMethod(string x, object y)
        {

        }

        public void MyNewMethod(object y, string x, string y2)
        {

        }
    }

    public class MethodExampleInvoker
    {
        public void Invoke()
        {
            var sample = new MethodExample();
            sample.MyOldMethod("text", 2);
        }
    }
}
