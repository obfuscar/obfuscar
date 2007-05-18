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
	public class SkipEnumTests
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

			string assemblyAPath = Path.Combine( inputPath, "AssemblyWithEnums.dll" );
			cp.OutputAssembly = assemblyAPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( inputPath, "AssemblyWithEnums.cs" ) );
			if ( cr.Errors.Count > 0 )
				Assert.Fail( "Unable to compile test assembly:  AssemblyWithEnums" );

			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );
		}

		void CheckEnums( string name, string[] expected, string[] notExpected )
		{
			AssemblyDefinition assmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, name ) );

			Assert.AreEqual( 2, assmDef.MainModule.Types.Count, "Should contain only one type, and <Module>." );

			C5.HashSet<string> valsToFind = new C5.HashSet<string>( );
			valsToFind.AddAll( expected );
			C5.HashSet<string> valsNotToFind = new C5.HashSet<string>( );
			valsNotToFind.AddAll( notExpected );

			bool foundType = false;
			foreach ( TypeDefinition typeDef in assmDef.MainModule.Types )
			{
				if ( typeDef.Name == "<Module>" )
					continue;
				else if ( typeDef.BaseType.FullName == "System.Enum" )
				{
					foundType = true;

					// num expected + num unexpected + field storage
					int totalValues = expected.Length + notExpected.Length + 1;
					Assert.AreEqual( totalValues, typeDef.Fields.Count,
						String.Format( "Type should have {0} values.", totalValues ) );

					foreach ( FieldDefinition field in typeDef.Fields )
					{
						Assert.IsFalse( valsNotToFind.Contains( field.Name ), String.Format(
							"Did not expect to find event '{0}'.", field.Name ) );

						valsToFind.Remove( field.Name );
					}
				}
			}

			Assert.IsFalse( valsToFind.Count > 0, "Failed to find all expected values." );

			Assert.IsTrue( foundType, "Should have found enum type." );
		}

		[Test]
		public void CheckRenamesEnumValues( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEnums.dll' />" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[0];

			string[] notExpected = new string[] {
				"Value1",
				"Value2",
				"ValueA"
			};

			CheckEnums( "AssemblyWithEnums.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipEnumsByName( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEnums.dll'>" +
				@"<SkipField type='TestClasses.Enum1' name='Value2' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Value2"
			};

			string[] notExpected = new string[] {
				"Value1",
				"ValueA"
			};

			CheckEnums( "AssemblyWithEnums.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipEventsByRx( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEnums.dll'>" +
				@"<SkipField type='TestClasses.Enum1' rx='Value\d' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Value1",
				"Value2"
			};

			string[] notExpected = new string[] {
				"ValueA"
			};

			CheckEnums( "AssemblyWithEnums.dll", expected, notExpected );
		}
	}
}
