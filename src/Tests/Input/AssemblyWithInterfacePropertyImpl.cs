using TestLib;

namespace TestClasses
{
    internal interface B : A
    {
    }

    internal class C : B
    {
        public int Property
        {
            get { return 0; }
        }

        public void Method()
        {
        }
    }

    public static class InterfacePropertyEntryPoint
    {
        public static int ExecuteThroughPublicInterface()
        {
            A value = new C();
            return value.Property;
        }
    }
}
