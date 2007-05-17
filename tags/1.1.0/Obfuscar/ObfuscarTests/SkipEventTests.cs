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
	public class SkipEventTests
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

			string assemblyAPath = Path.Combine( inputPath, "AssemblyWithEvents.dll" );
			cp.OutputAssembly = assemblyAPath;
			CompilerResults cr = provider.CompileAssemblyFromFile( cp, Path.Combine( inputPath, "AssemblyWithEvents.cs" ) );
			if ( cr.Errors.Count > 0 )
				Assert.Fail( "Unable to compile test assembly:  AssemblyWithEvents" );

			Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml( xml );

			obfuscator.RenameFields( );
			obfuscator.RenameParams( );
			obfuscator.RenameProperties( );
			obfuscator.RenameEvents( );
			obfuscator.RenameMethods( );
			obfuscator.RenameTypes( );
			obfuscator.SaveAssemblies( );
		}

		void CheckEvents( string name, string[] expected, string[] notExpected )
		{
			AssemblyDefinition assmDef = AssemblyFactory.GetAssembly(
				Path.Combine( outputPath, name ) );

			Assert.AreEqual( 3, assmDef.MainModule.Types.Count, "Should contain only one type, and <Module>." );

			C5.HashSet<string> eventsToFind = new C5.HashSet<string>( );
			eventsToFind.AddAll( expected );
			C5.HashSet<string> eventsNotToFind = new C5.HashSet<string>( );
			eventsNotToFind.AddAll( notExpected );

			C5.HashSet<string> methodsToFind = new C5.HashSet<string>( );
			foreach ( string evt in expected )
			{
				methodsToFind.Add( "add_" + evt );
				methodsToFind.Add( "remove_" + evt );
			}
			C5.HashSet<string> methodsNotToFind = new C5.HashSet<string>( );
			foreach ( string evt in notExpected )
			{
				methodsNotToFind.Add( "add_" + evt );
				methodsNotToFind.Add( "remove_" + evt );
			}

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

				Assert.AreEqual( expected.Length, typeDef.Events.Count,
					expected.Length == 1 ? "Type should have 1 event (others dropped by default)." :
					String.Format( "Type should have {0} events (others dropped by default).", expected.Length ) );

				// 2 methods / event + a method to fire them
				Assert.AreEqual( ( expected.Length + notExpected.Length ) * 2 + 1, typeDef.Methods.Count,
					"Some of the methods for the type are missing." );

				foreach ( EventDefinition evt in typeDef.Events )
				{
					Assert.IsFalse( eventsNotToFind.Contains( evt.Name ), String.Format(
						"Did not expect to find event '{0}'.", evt.Name ) );

					eventsToFind.Remove( evt.Name );
				}

				foreach ( MethodDefinition method in typeDef.Methods )
				{
					Assert.IsFalse( methodsNotToFind.Contains( method.Name ), String.Format(
						"Did not expect to find method '{0}'.", method.Name ) );

					methodsToFind.Remove( method.Name );
				}
			}

			Assert.IsFalse( eventsToFind.Count > 0, "Failed to find all expected events." );
			Assert.IsFalse( methodsToFind.Count > 0, "Failed to find all expected methods." );

			Assert.IsTrue( foundDelType, "Should have found the delegate type." );
			Assert.IsTrue( foundType, "Should have found non-<Module> type." );
		}

		[Test]
		public void CheckDropsEvents( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEvents.dll' />" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[0];

			string[] notExpected = new string[] {
				"Event1",
				"Event2",
				"EventA"
			};

			CheckEvents( "AssemblyWithEvents.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipEventsByName( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEvents.dll'>" +
				@"<SkipEvent type='TestClasses.ClassA' name='Event2' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Event2"
			};

			string[] notExpected = new string[] {
				"Event1",
				"EventA"
			};

			CheckEvents( "AssemblyWithEvents.dll", expected, notExpected );
		}

		[Test]
		public void CheckSkipEventsByRx( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithEvents.dll'>" +
				@"<SkipEvent type='TestClasses.ClassA' rx='Event\d' />" +
				@"</Module>" +
				@"</Obfuscator>", inputPath, outputPath );

			BuildAndObfuscateAssemblies( xml );

			string[] expected = new string[] {
				"Event1",
				"Event2"
			};

			string[] notExpected = new string[] {
				"EventA"
			};

			CheckEvents( "AssemblyWithEvents.dll", expected, notExpected );
		}
	}
}
