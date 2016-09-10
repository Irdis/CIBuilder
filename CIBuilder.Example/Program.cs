using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIBuilder.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var ci = new CompositeInterfaceBuilder();
            Type inter;
            var instance = ci.Build(new Dictionary<Type, object>()
            {
                { typeof(ISomething), new A() },
                { typeof(ISomething2), new B() },
            }, out inter);

            var method = instance.GetType().GetMethod("ISomething_Foo");
            var result = method.Invoke(instance, new object[] { 1, 2 });
            Console.WriteLine(result);

            method = instance.GetType().GetMethod("ISomething2_Bar");
            result = method.Invoke(instance, new object[] { 1, 2 });
            Console.WriteLine(result);
            Console.ReadKey();
        }
    }
}
