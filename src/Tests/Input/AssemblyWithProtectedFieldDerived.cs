namespace Issue601
{
    public class DerivedProtectedFieldReader : BaseWithProtectedField
    {
        public string ReadFromDerived()
        {
            return ProtectedMessage;
        }
    }

    public static class EntryPoint
    {
        public static string Execute()
        {
            var item = new DerivedProtectedFieldReader();
            return item.ReadFromBase() + ":" + item.ReadFromDerived();
        }
    }
}
