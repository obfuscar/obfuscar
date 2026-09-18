#nullable enable
namespace GenLib
{
    public interface IParent
    {
    }

    public interface IChild : IParent
    {
    }

    public class ServiceLocator
    {
        public T? GetService<T>()
            where T : class
        {
            return null;
        }
    }
}
