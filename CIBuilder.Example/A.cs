using System;

namespace CIBuilder.Example
{
    public class A : ISomething
    {
        public int Foo(int a, int b)
        {
            Console.WriteLine("Hello");
            return a + b;
        }

        public int Foo2(int a, int b)
        {
            return 3;
        }
    }
}