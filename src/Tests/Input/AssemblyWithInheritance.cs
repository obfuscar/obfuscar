using System;

namespace TestClasses
{
    public interface IMyInterface
    {
        void DoWork();
    }

    public class BaseClass : IMyInterface
    {
        public virtual void VirtualMethod() { }
        public void DoWork() { }
        public override string ToString() => "BaseClass";
        public override bool Equals(object obj) => true;
        public override int GetHashCode() => 42;
    }

    public class DerivedClass : BaseClass
    {
        public override void VirtualMethod() { }
        public override string ToString() => "DerivedClass";
    }

    public static class ClassWithStaticMethod
    {
        public static void StaticMethod() { }
    }
}
