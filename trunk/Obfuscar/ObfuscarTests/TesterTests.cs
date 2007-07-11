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
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace ObfuscarTests
{
	[TestFixture]
	public class TesterTests
	{
		[Test]
		public void TestTypeTester( )
		{
			Obfuscar.TypeKey key = new Obfuscar.TypeKey( "anything", "ns", "name" );

			Obfuscar.IPredicate<Obfuscar.TypeKey> tester;

			tester = new Obfuscar.TypeTester( "ns.name" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle strings." );
			tester = new Obfuscar.TypeTester( "ns.n*e" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle wildcards." );
			tester = new Obfuscar.TypeTester( "^ns\\.n.*e" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );
		}

		[Test]
		public void TestPropertyTester( )
		{
			Obfuscar.TypeKey typeKey = new Obfuscar.TypeKey( "anything", "ns", "name" );
			Obfuscar.PropertyKey key = new Obfuscar.PropertyKey( typeKey, "type", "property" );

			Obfuscar.IPredicate<Obfuscar.PropertyKey> tester;

			// check differnt kinds of name
			tester = new Obfuscar.PropertyTester( "property", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle strings." );
			tester = new Obfuscar.PropertyTester( "pr*ty", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle wildcards." );
			tester = new Obfuscar.PropertyTester( "^pr.*ty", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );
			tester = new Obfuscar.PropertyTester( new Regex( "p.*y" ), "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );

			// check differnt kinds of type name
			tester = new Obfuscar.PropertyTester( "property", "ns.n*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type wildcards." );
			tester = new Obfuscar.PropertyTester( "property", "^ns\\.n.*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type regular expressions." );
		}

		[Test]
		public void TestMethodTester( )
		{
			Obfuscar.TypeKey typeKey = new Obfuscar.TypeKey( "anything", "ns", "name" );
			Obfuscar.MethodKey key = new Obfuscar.MethodKey( typeKey, "method", new string[0] );

			Obfuscar.IPredicate<Obfuscar.MethodKey> tester;

			// check differnt kinds of name
			tester = new Obfuscar.MethodTester( "method", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle strings." );
			tester = new Obfuscar.MethodTester( "me*od", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle wildcards." );
			tester = new Obfuscar.MethodTester( "^me.*od", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );
			tester = new Obfuscar.MethodTester( new Regex( "me.*od" ), "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );

			// check differnt kinds of type name
			tester = new Obfuscar.MethodTester( "method", "ns.n*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type wildcards." );
			tester = new Obfuscar.MethodTester( "method", "^ns\\.n.*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type regular expressions." );
		}

		[Test]
		public void TestFieldTester( )
		{
			Obfuscar.TypeKey typeKey = new Obfuscar.TypeKey( "anything", "ns", "name" );
			Obfuscar.FieldKey key = new Obfuscar.FieldKey( typeKey, "type", "field" );

			Obfuscar.IPredicate<Obfuscar.FieldKey> tester;

			// check differnt kinds of name
			tester = new Obfuscar.FieldTester( "field", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle strings." );
			tester = new Obfuscar.FieldTester( "f*d", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle wildcards." );
			tester = new Obfuscar.FieldTester( "^f.*d", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );
			tester = new Obfuscar.FieldTester( new Regex( "f.*d" ), "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );

			// check differnt kinds of type name
			tester = new Obfuscar.FieldTester( "field", "ns.n*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type wildcards." );
			tester = new Obfuscar.FieldTester( "field", "^ns\\.n.*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type regular expressions." );
		}

		[Test]
		public void TestEventTester( )
		{
			Obfuscar.TypeKey typeKey = new Obfuscar.TypeKey( "anything", "ns", "name" );
			Obfuscar.EventKey key = new Obfuscar.EventKey( typeKey, "type", "event" );

			Obfuscar.IPredicate<Obfuscar.EventKey> tester;

			// check differnt kinds of name
			tester = new Obfuscar.EventTester( "event", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle strings." );
			tester = new Obfuscar.EventTester( "e*t", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle wildcards." );
			tester = new Obfuscar.EventTester( "^e.*t", "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );
			tester = new Obfuscar.EventTester( new Regex( "e.*t" ), "ns.name", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle regular expressions." );

			// check differnt kinds of type name
			tester = new Obfuscar.EventTester( "event", "ns.n*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type wildcards." );
			tester = new Obfuscar.EventTester( "event", "^ns\\.n.*e", "any" );
			Assert.IsTrue( tester.Test( key ), "Tester should handle type regular expressions." );
		}
	}
}
