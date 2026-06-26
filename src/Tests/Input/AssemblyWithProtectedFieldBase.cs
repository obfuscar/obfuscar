namespace Issue601
{
    public class BaseWithProtectedField
    {
        protected readonly string ProtectedMessage = "protected-field";

        public string ReadFromBase()
        {
            return ProtectedMessage;
        }
    }
}
