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
using System.CodeDom.Compiler;

using NUnit.Framework;
using Mono.Cecil;

namespace ObfuscarTests
{
	[TestFixture]
	public class SigningTests
	{
		[Test]
		public void CheckCannotObfuscateSigned( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyForSigning.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath );

			TestHelper.CleanInput( );

			// build it with the keyfile option (embeds the public key, and signs the assembly)
			TestHelper.BuildAssembly( "AssemblyForSigning", String.Empty, "/keyfile:" + TestHelper.InputPath + @"\SigningKey.snk" );

			TestUtils.AssertThrows( delegate { TestHelper.Obfuscate( xml ); }, typeof( ApplicationException ),
				"signed assembly", "invalid", "AssemblyForSigning" );
		}

		[Test]
		public void CheckCanObfuscateDelaySigned( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyForSigning.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath );

			TestHelper.CleanInput( );

			// build it with the delaysign option (embeds the public key, reserves space for the signature, but does not sign)
			TestHelper.BuildAssembly( "AssemblyForSigning", String.Empty, "/delaysign /keyfile:" + TestHelper.InputPath + @"\SigningKey.snk" );

			// this should not throw
			TestHelper.Obfuscate( xml );
		}
	}
}
