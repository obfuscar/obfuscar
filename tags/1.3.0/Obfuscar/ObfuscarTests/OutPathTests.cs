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
using System.IO;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace ObfuscarTests
{
	[TestFixture]
	public class OutPathTests
	{
		private void CheckOutPath( string testPath )
		{
			Assert.IsFalse( Directory.Exists( testPath ), "Need a writeable temp path...wanted to create " + testPath );

			try
			{
				string xml = String.Format(
					@"<?xml version='1.0'?>" +
					@"<Obfuscator>" +
					@"<Var name='OutPath' value='{0}' />" +
					@"</Obfuscator>", testPath );

				Obfuscar.Obfuscator.CreateFromXml( xml );

				Assert.IsTrue( Directory.Exists( testPath ), "Obfuscator should have created its missing OutPath." );
			}
			finally
			{
				// clean up...
				if ( Directory.Exists( testPath ) )
					Directory.Delete( testPath );
			}
		}

		[Test]
		public void CheckCanCreateOutPath( )
		{
			string testPath = System.IO.Path.Combine( System.IO.Path.GetTempPath( ), "ObfuscarTestOutPath" );

			CheckOutPath( testPath );
		}

		[Test]
		public void CheckInvalidOutPath( )
		{
			string testPath = System.IO.Path.Combine( PathFailureTests.BadPath, "ObfuscarTestOutPath" );

			TestUtils.AssertThrows( delegate { CheckOutPath( testPath ); }, typeof( ApplicationException ),
				"Could not create", "OutPath", testPath );
		}
	}
}
