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
using System.IO;
using System.CodeDom.Compiler;

using NUnit.Framework;

namespace ObfuscarTests
{
	static class TestHelper
	{
		public const string InputPath = "..\\..\\Input";
		public const string OutputPath = "..\\..\\Output";

		public static void CleanInput( )
		{
			// clean out inputPath
			foreach ( string file in Directory.GetFiles( InputPath, "*.dll" ) )
				File.Delete( file );
		}

		public static void BuildAssembly( string name, string suffix )
		{
			BuildAssembly( name, suffix, null );
		}

		public static void BuildAssembly( string name, string suffix, string options )
		{
			Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider( );

			CompilerParameters cp = new CompilerParameters( );
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = false;
			cp.TreatWarningsAsErrors = true;

			if ( !String.IsNullOrEmpty( options ) )
				cp.CompilerOptions = options;

			string dllName = String.IsNullOrEmpty( suffix ) ? name : name + suffix;

			string assemblyPath = Path.Combine( InputPath, dllName + ".dll" );
			cp.OutputAssembly = assemblyPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( InputPath, name + ".cs" ) );
			if ( cr.Errors.HasErrors )
				Assert.Fail( "Unable to compile test assembly:  " + dllName );
		}

		public static Obfuscar.Obfuscator Obfuscate( string xml )
		{
			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );

			return obfuscator;
		}

		public static Obfuscar.Obfuscator BuildAndObfuscate( string name, string suffix, string xml )
		{
			CleanInput( );
			BuildAssembly( name, suffix );
			return Obfuscate( xml );
		}
	}
}
