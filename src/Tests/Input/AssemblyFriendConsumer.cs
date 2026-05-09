namespace TestClasses
{
    // Friend assembly that accesses internals of AssemblyWithInternalsVisibleTo.
    public class FriendConsumer
    {
        public string GetValue()
        {
            var obj = new InternalSharedClass();
            obj.SharedMethod("hello");
            return obj.SharedProp;
        }
    }
}
