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
using Mono.Cecil;

namespace ObfuscarTests
{
	public class SkipMethodTestBase
	{
		protected const string inputPath = "..\\..\\Input";
		protected const string outputPath = "..\\..\\Output";

		protected void BuildAndObfuscateAssemblies( string name, string suffix, string xml )
		{
			// clean out inputPath
			foreach ( string file in Directory.GetFiles( inputPath, "*.dll" ) )
				File.Delete( file );

			Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider( );

			CompilerParameters cp = new CompilerParameters( );
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = false;
			cp.TreatWarningsAsErrors = true;

			string dllName = String.IsNullOrEmpty( suffix )? name : name + suffix;

			string assemblyPath = Path.Combine( inputPath, dllName + ".dll" );
			cp.OutputAssembly = assemblyPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( inputPath, name + ".cs" ) );
			if ( cr.Errors.HasErrors )
				Assert.Fail( "Unable to compile test assembly:  " + dllName );

			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );
		}

		protected void CheckMethods( string name, int expectedTypes, string[] expected, string[] notExpected,
			Predicate<TypeDefinition> isType, Action<TypeDefinition> checkType )
		{
			AssemblyDefinition assmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, name + ".dll" ) );

			Assert.AreEqual( expectedTypes + 1, assmDef.MainModule.Types.Count,
				String.Format( "Should contain only {0} types, and <Module>.", expectedTypes ) );

			C5.HashSet<string> methodsToFind = new C5.HashSet<string>( );
			methodsToFind.AddAll( expected );
			C5.HashSet<string> methodsNotToFind = new C5.HashSet<string>( );
			methodsNotToFind.AddAll( notExpected );

			bool foundType = false;
			foreach ( TypeDefinition typeDef in assmDef.MainModule.Types )
			{
				if ( typeDef.Name == "<Module>" )
					continue;
				else if ( isType( typeDef ) )
				{
					foundType = true;

					// make sure we have enough methods...
					Assert.AreEqual( expected.Length + notExpected.Length, typeDef.Methods.Count,
						"Some of the methods for the type are missing." );

					foreach ( MethodDefinition method in typeDef.Methods )
					{
						Assert.IsFalse( methodsNotToFind.Contains( method.Name ), String.Format(
							"Did not expect to find method '{0}'.", method.Name ) );

						methodsToFind.Remove( method.Name );
					}

					if ( checkType != null )
						checkType( typeDef );
				}
			}

			Assert.IsFalse( methodsToFind.Count > 0, "Failed to find all expected methods." );
			Assert.IsTrue( foundType, "Should have found non-<Module> type." );
		}
	}
}
