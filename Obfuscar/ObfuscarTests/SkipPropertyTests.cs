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
	public class SkipPropertyTests
	{
		const string inputPath = "..\\..\\Input";
		const string outputPath = "..\\..\\Output";

		void BuildAndObfuscateAssemblies( string xml )
		{
			// clean out inputPath
			foreach ( string file in Directory.GetFiles( inputPath, "*.dll" ) )
				File.Delete( file );

			Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider( );

			CompilerParameters cp = new CompilerParameters( );
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = false;
			cp.TreatWarningsAsErrors = true; ;

			string assemblyAPath = Path.Combine( inputPath, "AssemblyWithProperties.dll" );
			cp.OutputAssembly = assemblyAPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( inputPath, "AssemblyWithProperties.cs" ) );
			if ( cr.Errors.Count > 0 )
				Assert.Fail( "Unable to compile test assembly:  AssemblyWithProperties" );

			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );
		}

		void CheckProperties( string name, string[] expected, string[] notExpected )
		{
			AssemblyDefinition assmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, name ) );

			Assert.AreEqual( 2, assmDef.MainModule.Types.Count, "Should contain only one type, and <Module>." );

			C5.HashSet<string> propsToFind = new C5.HashSet<string>( );
			propsToFind.AddAll( expected );
			C5.HashSet<string> propsNotToFind = new C5.HashSet<string>( );
			propsNotToFind.AddAll( notExpected );

			C5.HashSet<string> methodsToFind = new C5.HashSet<string>( );
			foreach ( string prop in expected )
			{
				methodsToFind.Add( "get_" + prop );
				methodsToFind.Add( "set_" + prop );
			}
			C5.HashSet<string> methodsNotToFind = new C5.HashSet<string>( );
			foreach ( string prop in notExpected )
			{
				methodsNotToFind.Add( "get_" + prop );
				methodsNotToFind.Add( "set_" + prop );
			}

			bool foundType = false;
			foreach ( TypeDefinition typeDef in assmDef.MainModule.Types )
			{
				if ( typeDef.Name == "<Module>" )
					continue;
				else
					foundType = true;

				Assert.AreEqual( expected.Length, typeDef.Properties.Count,
					expected.Length == 1 ? "Type should have 1 property (others dropped by default)." :
					String.Format( "Type should have {0} properties (others dropped by default).", expected.Length ) );

				// 2 methods / property
				Assert.AreEqual( ( expected.Length + notExpected.Length ) * 2, typeDef.Methods.Count,
					"Some of the methods for the type are missing." );

				foreach ( PropertyDefinition prop in typeDef.Properties )
				{
					Assert.IsFalse( propsNotToFind.Contains( prop.Name ), String.Format(
						"Did not expect to find property '{0}'.", prop.Name ) );

					propsToFind.Remove( prop.Name );
				}

				foreach ( MethodDefinition method in typeDef.Methods )
				{
					Assert.IsFalse( methodsNotToFind.Contains( method.Name ), String.Format(
						"Did not expect to find method '{0}'.", method.Name ) );

					methodsToFind.Remove( method.Name );
				}
			}

			Assert.IsFalse( propsToFind.Count > 0, "Failed to find all expected properties." );
			Assert.IsFalse( methodsToFind.Count > 0, "Failed to find all expected methods." );

			Assert.IsTrue( foundType, "Should have found non-<Module> type." );
		}

		[Test]
		public void CheckDropsProperties( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithProperties.dll' />" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[0];

			string[] notExpected = new string[] {
				"Property1",
				"Property2",
				"PropertyA"
			};

			CheckProperties( "AssemblyWithProperties.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipPropertyByName( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithProperties.dll'>" +
				@"<SkipProperty type='TestClasses.ClassA' name='Property2' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Property2"
			};

			string[] notExpected = new string[] {
				"Property1",
				"PropertyA"
			};

			CheckProperties( "AssemblyWithProperties.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipPropertyByRx( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithProperties.dll'>" +
				@"<SkipProperty type='TestClasses.ClassA' rx='Property\d' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Property1",
				"Property2"
			};

			string[] notExpected = new string[] {
				"PropertyA"
			};

			CheckProperties( "AssemblyWithProperties.dll", expected, notExpected );
		}
	}
}
