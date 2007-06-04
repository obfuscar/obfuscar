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
	public class SpecializedGenericsTests
	{
		const string inputPath = "..\\..\\Input";
		const string outputPath = "..\\..\\Output";

		Obfuscar.ObfuscationMap BuildAndObfuscateAssemblies( )
		{
			// clean out inputPath
			foreach ( string file in Directory.GetFiles( inputPath, "*.dll" ) )
				File.Delete( file );

			Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider( );

			CompilerParameters cp = new CompilerParameters( );
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = false;
			cp.TreatWarningsAsErrors = true; ;

			string assemblyAPath = Path.Combine( inputPath, "AssemblyWithSpecializedGenerics.dll" );
			cp.OutputAssembly = assemblyAPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( inputPath, "AssemblyWithSpecializedGenerics.cs" ) );
			if ( cr.Errors.Count > 0 )
				Assert.Fail( "Unable to compile test assembly:  AssemblyWithSpecializedGenerics" );

			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithSpecializedGenerics.dll' />" +
				@"</Obfuscator>", inputPath, outputPath );

			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );

			return obfuscator.Mapping;
		}

		void CheckMethods( string name, string[] expected, string[] notExpected )
		{
			AssemblyDefinition assmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, name ) );

			Assert.AreEqual( 3, assmDef.MainModule.Types.Count, "Should contain only two types, and <Module>." );

			C5.HashSet<string> methodsToFind = new C5.HashSet<string>( );
			methodsToFind.AddAll( expected );
			C5.HashSet<string> methodsNotToFind = new C5.HashSet<string>( );
			methodsNotToFind.AddAll( notExpected );

			bool foundType = false;
			bool foundDelType = false;
			foreach ( TypeDefinition typeDef in assmDef.MainModule.Types )
			{
				if ( typeDef.Name == "<Module>" )
					continue;
				else if ( typeDef.BaseType.FullName == "System.MulticastDelegate" )
				{
					foundDelType = true;
					continue;
				}
				else
					foundType = true;

				// 2 methods / event + a method to fire them
				Assert.AreEqual( expected.Length + notExpected.Length, typeDef.Methods.Count,
					"Some of the methods for the type are missing." );

				foreach ( MethodDefinition method in typeDef.Methods )
				{
					Assert.IsFalse( methodsNotToFind.Contains( method.Name ), String.Format(
						"Did not expect to find method '{0}'.", method.Name ) );

					methodsToFind.Remove( method.Name );
				}
			}

			Assert.IsFalse( methodsToFind.Count > 0, "Failed to find all expected methods." );

			Assert.IsTrue( foundDelType, "Should have found the delegate type." );
			Assert.IsTrue( foundType, "Should have found non-<Module> type." );
		}

		MethodDefinition FindByName( TypeDefinition typeDef, string name )
		{
			foreach ( MethodDefinition method in typeDef.Methods )
				if ( method.Name == name )
					return method;

			Assert.Fail( String.Format( "Expected to find method: {0}", name ) );
			return null; // never here
		}

		[Test]
		public void CheckClassHasAttribute( )
		{
			Obfuscar.ObfuscationMap map = BuildAndObfuscateAssemblies( );

			string assmName = "AssemblyWithSpecializedGenerics.dll";

			AssemblyDefinition inAssmDef = AssemblyFactory.GetAssembly(
				Path.Combine( inputPath, assmName ) );
			
			AssemblyDefinition outAssmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, assmName ) );

			TypeDefinition classAType = inAssmDef.MainModule.Types["TestClasses.ClassA`1"];
			MethodDefinition classAmethod2 = FindByName( classAType, "Method2" );

			TypeDefinition classBType = inAssmDef.MainModule.Types["TestClasses.ClassB"];
			MethodDefinition classBmethod2 = FindByName( classBType, "Method2" );

			Obfuscar.ObfuscatedThing classAEntry = map.GetMethod( new Obfuscar.MethodKey( classAmethod2 ) );
			Obfuscar.ObfuscatedThing classBEntry = map.GetMethod( new Obfuscar.MethodKey( classBmethod2 ) );

			Assert.IsTrue(
				classAEntry.Status == Obfuscar.ObfuscationStatus.Renamed &&
				classBEntry.Status == Obfuscar.ObfuscationStatus.Renamed,
				"Both methods should have been renamed." );

			Assert.IsTrue(
				classAEntry.StatusText == classBEntry.StatusText,
				"Both methods should have been renamed to the same thing." );
		}
	}
}
