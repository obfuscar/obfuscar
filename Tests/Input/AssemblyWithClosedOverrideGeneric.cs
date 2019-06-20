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

using System;
using System.Collections;
using System.Collections.Generic;

namespace TestClasses
{
    public interface IFoo { }
    public interface IFoos : IEnumerable<IFoo>
    {
        IFoo[] ToArray ();
        int Test(IFoo foo);
    }
    public class ClosedFoos : IFoos
    {
        public IFoo[] ToArray ()
        {
            throw new NotImplementedException ();
        }
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
        public IEnumerator<IFoo> GetEnumerator ()
        {
            throw new NotImplementedException ();
        }
        public int Test(IFoo foo)
        {
            throw new NotImplementedException();
        }
    }
    public abstract class Generic<T> : IEnumerable<T> where T : class, IFoo
    {
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator();
        }
        public abstract IEnumerator<T> GetEnumerator ();
        public void Some ()
        {
        }
        public void Other ()
        {
        }
        public void Method ()
        {
        }
        public T[] ToArray () // This is required by IFoos
        {
            throw new NotImplementedException ();
        }
        public int Test(T t)
        {
            throw new NotImplementedException();
        }
    }
    public class Foos : Generic<IFoo>
    {
        public override IEnumerator<IFoo> GetEnumerator ()
        {
            throw new NotImplementedException ();
        }
    }
    public class ChildFoos : Foos, IFoos
    {
    }
}
