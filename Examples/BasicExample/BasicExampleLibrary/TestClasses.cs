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

namespace BasicExampleLibrary
{
	public interface Interface1
	{
		int MethodA( int param1 );
		int MethodB( int param1 );
		int Method1( int param1 );
		int Method2( int param1 );
	}

	public class InterfaceOverride1 : Interface1
	{
		public int MethodA( int param1 ) { return param1; }
		public int MethodB( int param1 ) { return param1; }
		public int Method1( int param1 ) { return param1; }
		public int Method2( int param1 ) { return param1; }
	}

	public class Base1
	{
		public int MethodA( int param1 ) { return param1; }
		public int MethodB( int param1 ) { return param1; }
		public virtual int Method1( int param1 ) { return param1; }
		public int Method2( int param1 ) { return param1; }
	}

	public class InterfaceOverride2 : Base1, Interface1
	{
		public virtual int MethodX( int param1 ) { return param1; }
		public override int Method1( int param1 ) { return param1; }
	}
}
