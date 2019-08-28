using System;
using System.Collections.Generic;
using System.Text;

namespace Samples
{

    class PropertyExampleCaller
    {
        public void Invoke()
        {
            var myClass = new PropertyExample();
            myClass.MyOldProperty = 2;
            var x = myClass.MyOldProperty;
        }
    }

    class PropertyExample
    {

        [Obsolete("Replace with `MyNewProperty`")]
        public int MyOldProperty { get; set; }

        public int MyNewProperty { get; set; }
    }

}
