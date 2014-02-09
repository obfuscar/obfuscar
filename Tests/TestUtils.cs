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

using NUnit.Framework;

namespace ObfuscarTests
{
	static class TestUtils
	{
		public delegate void MethodInvoker( );

		public static void AssertThrows( MethodInvoker del, Type exType, params string[] containedStrings )
		{
			bool threw = false;

			try
			{
				del.DynamicInvoke( null );
			}
			catch ( Exception ex )
			{
				threw = true;

				Assert.IsTrue( exType.IsInstanceOfType( ex.InnerException ),
					String.Format( "Exception was expected to be of type {0}.", exType.Name ) );
				foreach ( string containedString in containedStrings )
					StringAssert.Contains( containedString, ex.InnerException.Message,
						String.Format( "Exception string was expected to contain \"{0}\"", containedString ) );
			}

			if ( !threw )
				Assert.Fail( "An exception was expected." );
		}
	}
}
