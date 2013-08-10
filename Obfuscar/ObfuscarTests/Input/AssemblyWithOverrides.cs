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
	internal class ClassA : IComparable
	{
		protected virtual void Method1( int param )
		{
		}

		protected virtual void Method2( int param )
		{
		}

		public int CompareTo(object other)
		{
			return 0;
		}
	}

	internal class ClassB : ClassA, IComparable<ClassB>
	{
		protected override void Method2( int param )
		{
		}

		public int CompareTo(ClassB other)
		{
			return 0;
		}
	}

	public class ClassC
	{
		protected virtual void Method1 (int param)
		{			
		}
	}

	public class ClassE : ClassC
	{ 
	}

	internal class ClassD : ClassE
	{
		protected override void Method1 (int param)
		{			
		}
	}

	internal abstract class ClassF
	{
		protected abstract int Test();
	}

	internal class ClassG
	{
		protected virtual int Test()
		{
			return 1;
		}
	}
}
