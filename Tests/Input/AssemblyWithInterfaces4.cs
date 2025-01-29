using System;
using System.Collections.Generic;
using System.Text;

namespace TestClasses
{
    interface IFace2
    {
    }

    interface IFace<T> : IFace2
    {
        T Value { set; }
        T Value3 { set; }
    }

    abstract class AbstractImpl<T> : IFace<T>
    {
        public abstract T Value { set; }
        public T Value3
        {
            set
            {
                throw new Exception("Wrong call! Value3 = " + value);
            }
        }
    }

    abstract class Impl1 : AbstractImpl<int>
    {
        public override int Value
        {
            set
            {
                throw new NotImplementedException();
            }
        }
    }

    class Impl2<T> : AbstractImpl<T>
    {
        public override T Value
        {
            set
            {
                throw new Exception("Wrong call! Value = " + value);
            }
        }

        public T Value2 { get; set; }

        public T Value4 { set { Value3 = value; } }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var x = new Impl2<int>();
            x.Value2 = 42;
        }
    }
}
