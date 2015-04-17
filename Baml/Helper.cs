namespace ICSharpCode.ILSpy
{
    class Helper
    {
    }
}

#if __MonoCS__
namespace System.Windows.Media
{
    public class Int32Collection
    {
        public Int32Collection(int count)
        {
        }

        public void Add(object obj)
        {
        }
    }

    public class BrushConverter
    {
        public string ConvertToString(object obj)
        {
            return null;
        }
    }

    public class Int32CollectionConverter
    {
        public string ConvertToString(object obj)
        {
            return null;
        }
    }

    public class SolidColorBrush
    {
        public static object DeserializeFrom(object obj)
        {
            return null;
        }
    }
}
#endif