using System.ComponentModel;
using System.Collections.Generic;

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

    public abstract class HeirToAbstractBaseClassImplementsINotifyPropertyChanged
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }
    public abstract class HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }
    public class HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }
    public class HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged
    {
        public int PublicProperty { get { return 1; } }
        protected int ProtectedProperty { get { return 2; } }
        internal int InternalProperty { get { return 3; } }
        private int PrivateProperty { get { return 4; } }
    }

    public class V : HeirToAbstractBaseClassImplementsINotifyPropertyChanged, E<int>
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
    }

    public class X : HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged, L<int> { }

    public class Y : HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged, E<int>
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
    }

    public class Z : HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged, L<int> { }
}
