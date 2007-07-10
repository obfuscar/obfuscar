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
		Obfuscar.ObfuscationMap BuildAndObfuscateAssemblies( )
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithSpecializedGenerics.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath );

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate( "AssemblyWithSpecializedGenerics", String.Empty, xml );

			return obfuscator.Mapping;
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
				Path.Combine( TestHelper.InputPath, assmName ) );
			
			AssemblyDefinition outAssmDef = AssemblyFactory.GetAssembly(
				Path.Combine( TestHelper.OutputPath, assmName ) );

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
