using System.ComponentModel;

namespace TestClasses
{
    public interface A : INotifyPropertyChanged
    {
    }

    internal interface B : A
    {
    }

    public interface C<T> : A
    {
    }

    public interface D<T> : C<T>
    {
    }

    public interface E
    {
    }

    public interface F : E
    {
    }
    public interface G<T> : E
    {
    }

    public interface H<T> : G<T>
    {
    }

    internal class ImplementsINotifyPropertyChanged : INotifyPropertyChanged
    {
#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067
    }

    internal class DoesNotImplementsINotifyPropertyChanged
    {
    }

    internal class X : ImplementsINotifyPropertyChanged, D<int>
    {
    }

    internal class Y : DoesNotImplementsINotifyPropertyChanged, H<int>
    {
    }

    internal class Z : D<int>
    {
#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067
    }
}
