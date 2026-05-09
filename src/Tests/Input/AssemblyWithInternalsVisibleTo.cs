using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AssemblyFriendConsumer")]

namespace TestClasses
{
    // Internal class whose members are exposed to AssemblyFriendConsumer via InternalsVisibleTo.
    internal class InternalSharedClass
    {
        public string SharedProp { get; set; }
        public int SharedField;
        public void SharedMethod(string value) { SharedProp = value; }
    }

    public static class InternalSharedFactory
    {
        public static object Create() => new InternalSharedClass();
    }
}
