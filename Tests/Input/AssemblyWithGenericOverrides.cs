#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>
#endregion

namespace TestClasses
{
    public interface IAlpha<T> { }
    public interface IBeta<T1, T2> { }
    public class Alpha<T> : IAlpha<T>
    {
        public Alpha (T i)
        {
            Item = i;
        }
        public T Item;
        public override string ToString ()
        {
            return string.Format ("A<{0}>", Item);
        }
    }
    public class Beta<T1, T2> : IBeta<T1, T2>
    {
        public override string ToString()
        {
            return string.Format ("B<{0}, {1}>", typeof(T1).Name, typeof(T2).Name);
        }
    }
    public static class Generic
    {
        public static IAlpha<IBeta<C, int>> Empty<C> ()
        {
            return Empty<C, int> ();
        }
        public static IAlpha<IBeta<C, D>> Empty<C, D> ()
        {
            return new Alpha<IBeta<C, D>> ((IBeta<C, D>) new Beta<C, D> ());
        }
    }
}
