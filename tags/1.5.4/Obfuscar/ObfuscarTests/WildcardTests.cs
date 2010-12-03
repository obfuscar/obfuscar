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

using NUnit.Framework;

namespace ObfuscarTests
{
	[TestFixture]
	public class WildcardTests
	{
		[Test]
		public void TestEmptyString( )
		{
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "", "pattern" ) );
		}

		[Test]
		public void TestEmptyPattern( )
		{
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "test", "" ) );
		}

		[Test]
		public void BasicTests( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "*", "*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "?", "?" ) );

			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "testo", "test" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "test", "testo" ) );
		}

		[Test]
		public void TestOneQuestionMark( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som?thing" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "somthing", "som?thing" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "someething", "som?thing" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "s", "?" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "so", "?" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "so", "?o" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "s", "?o" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "o", "?o" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "os", "?o" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "so", "s?" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "s", "s?" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "o", "s?" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "os", "s?" ) );
		}

		[Test]
		public void TestTwoQuestionMarks( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som??hing" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "somhing", "som??hing" ) );
		}

		[Test]
		public void TestSpacedQuestionMarks( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som?t?ing" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "somting", "som?t?ing" ) );
		}

		[Test]
		public void TestOneStar( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som*thing" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "somthing", "som*thing" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "someething", "som*thing" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "", "*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "s", "*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "so", "*" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "so", "*o" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "s", "*o" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "o", "*o" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "os", "*o" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "so", "s*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "s", "s*" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "o", "s*" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "os", "s*" ) );
		}

		[Test]
		public void TestTwoStars( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som**hing" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "somhing", "som**hing" ) );
		}

		[Test]
		public void TestSpacedStars( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som*t*ing" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "somting", "som*t*ing" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "someethhing", "som*t*ing" ) );
			Assert.IsFalse( Obfuscar.Helper.MatchWithWildCards( "samething", "som*t*ing" ) );

			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "a", "*a*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "an", "*a*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "na", "*a*" ) );
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "nan", "*a*" ) );
		}

		[Test]
		public void TestBasicMixed( )
		{
			Assert.IsTrue( Obfuscar.Helper.MatchWithWildCards( "something", "som?t*g" ) );
		}
	}
}
