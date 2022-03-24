using System.ComponentModel;

namespace TestClasses
{
    public interface A : INotifyPropertyChanged { }
    public interface B : A { }
    public interface C<T> : A { }
    public interface D<T> : C<T> { }
    public interface E<T> : C<T> { }

    public interface H { }
    public interface I : H { }
    public interface J<T> : H { }
    public interface K<T> : J<T> { }
    public interface L<T> : J<T> { }

    public class NotifyPropertyChangedImpl : E<int>
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
    }
    
    public class ImplementsINotifyPropertyChanged : E<int>
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }

    public class BaseImplementsINotifyPropertyChanged : NotifyPropertyChangedImpl
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }

    public class NotImplementsINotifyPropertyChanged : L<int>
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }
}
