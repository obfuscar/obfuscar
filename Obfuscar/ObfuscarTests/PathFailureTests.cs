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
	[TestFixture]
	public class PathFailureTests
	{
		internal const string BadPath = "Q:\\Does\\Not\\Exist";

		[Test]
		public void CheckBadPathIsBad( )
		{
			// if badPath exists, the other tests here are no good
			Assert.IsFalse( System.IO.Directory.Exists( BadPath ), "Didn't expect BadPath to exist." );
		}

		[Test]
		public void CheckBadProjectPath( )
		{
			TestUtils.AssertThrows( delegate { new Obfuscar.Obfuscator( BadPath ); }, typeof( ApplicationException ),
				"Unable", "read", BadPath );
		}

		[Test]
		public void CheckBadModuleFile( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Module file='{0}\ObfuscarTests.dll' />" +
				@"</Obfuscator>", BadPath );

			TestUtils.AssertThrows( delegate { Obfuscar.Obfuscator.CreateFromXml( xml ); }, typeof( ApplicationException ),
				"Unable", "find assembly", BadPath );
		}

		[Test]
		public void CheckBadInPath( )
		{
			string xml = String.Format( 
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"</Obfuscator>", BadPath );

			TestUtils.AssertThrows( delegate { Obfuscar.Obfuscator.CreateFromXml( xml ); }, typeof( ApplicationException ),
				"InPath", "must exist", BadPath );
		}
	}
}
