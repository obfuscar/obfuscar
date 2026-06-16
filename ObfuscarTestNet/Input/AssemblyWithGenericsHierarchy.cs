using System.Reflection;

namespace ObfuscarTestNet.Input
{
    /// <summary> Interface for testing proper generic method grouping. </summary>
    public interface IBaseInterface<T>
    {
        /// <summary> This method would normally go into an int param method grouping. </summary>
        void Method(int index, T value);

        /// <summary> This method would normally go into a string param method grouping. </summary>
        void Method(string key, T value);
    }

    /// <summary> Class obfuscated by default. </summary>
    public class BaseClass1<T> : IBaseInterface<T>
    {
        /// <summary> This method should be named same as base method after obfuscation. </summary>
        public virtual void Method(int index, T value)
        {
        }

        /// <summary> This method should be named same as base method after obfuscation. </summary>
        public virtual void Method(string key, T value)
        {
        }
    }

    /// <summary> Class excluded from obfuscation. </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class BaseClass2<T> : IBaseInterface<T>
    {
        /// <summary> This method would normally cause all int param methods to be skipped. </summary>
        public virtual void Method(int index, T value)
        {
        }

        /// <summary> This method would normally cause all string param methods to be skipped. </summary>
        public virtual void Method(string key, T value)
        {
        }
    }

    /// <summary> Derived class excluded from obfuscation. </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class Class2<T, V> : BaseClass2<T>
    {
        /// <summary> Due to this method having two generic arguments it should be renamed same as both <see cref="IBaseInterface{T}"/> methods
        /// for proper overload resolution, effectively merging the int and string parameter groups. But due to a bug in ThreeShape.Obfuscar v4.5.0,
        /// the group merging fails and as a result one of the overloads in <see cref="BaseClass1{T}"/> gets renamed anyway
        /// which leads to the method implementation being impossible to resolve at runtime. </summary>
        public void Method(V key, T value)
        {
        }
    }
}
