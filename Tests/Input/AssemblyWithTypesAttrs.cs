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
using System.Collections.Generic;
using System.Text;

namespace TestClasses
{
	internal class InternalClass2 // yes
	{
        [System.Reflection.ObfuscationAttribute(Exclude = true)]
        public void PublicMethod() // no
        { }

		public class PublicNestedClass // yes
		{
		     public class PublicNestedClass2
			 {
			 }
		}
	}

    [System.Reflection.ObfuscationAttribute(Exclude = true)]
    internal class InternalClass // no
    {
        public void PublicMethod() // no
        { }

        public class PublicNestedClass // yes
        {
			public class PublicNestedClass2 // yes
			{
			}
        }
    }

    [System.Reflection.ObfuscationAttribute(Exclude=false, ApplyToMembers=true)]
    public class PublicClass // yes
    {
        private void PrivateMethod() // yes
        { }

        public void PublicMethod() // yes
        { }

        protected void ProtectedMethod() // yes
        { }
    }

    [System.Reflection.ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    public class PublicClass2 // no
    {
        private void PrivateMethod() // no
        { }

        [System.Reflection.ObfuscationAttribute(Exclude = false)]
        public void PublicMethod() // yes
        { }

        protected void ProtectedMethod() // no
        { }
    }
}
