#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

#endregion


using Xunit;

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
namespace ObfuscarTest
{
    public class WildcardTests
    {
        [Fact]
        public void TestEmptyString()
        {
            Assert.False(Obfuscar.Helper.MatchWithWildCards("", "pattern"));
        }

        [Fact]
        public void TestEmptyPattern()
        {
            Assert.False(Obfuscar.Helper.MatchWithWildCards("test", ""));
        }

        [Fact]
        public void BasicTests()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("*", "*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("?", "?"));

            Assert.False(Obfuscar.Helper.MatchWithWildCards("testo", "test"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("test", "testo"));
        }

        [Fact]
        public void TestOneQuestionMark()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som?thing"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("somthing", "som?thing"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("someething", "som?thing"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("s", "?"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("so", "?"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("so", "?o"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("s", "?o"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("o", "?o"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("os", "?o"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("so", "s?"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("s", "s?"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("o", "s?"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("os", "s?"));
        }

        [Fact]
        public void TestTwoQuestionMarks()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som??hing"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("somhing", "som??hing"));
        }

        [Fact]
        public void TestSpacedQuestionMarks()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som?t?ing"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("somting", "som?t?ing"));
        }

        [Fact]
        public void TestOneStar()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som*thing"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("somthing", "som*thing"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("someething", "som*thing"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("", "*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("s", "*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("so", "*"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("so", "*o"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("s", "*o"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("o", "*o"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("os", "*o"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("so", "s*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("s", "s*"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("o", "s*"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("os", "s*"));
        }

        [Fact]
        public void TestTwoStars()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som**hing"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("somhing", "som**hing"));
        }

        [Fact]
        public void TestSpacedStars()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som*t*ing"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("somting", "som*t*ing"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("someethhing", "som*t*ing"));
            Assert.False(Obfuscar.Helper.MatchWithWildCards("samething", "som*t*ing"));

            Assert.True(Obfuscar.Helper.MatchWithWildCards("a", "*a*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("an", "*a*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("na", "*a*"));
            Assert.True(Obfuscar.Helper.MatchWithWildCards("nan", "*a*"));
        }

        [Fact]
        public void TestBasicMixed()
        {
            Assert.True(Obfuscar.Helper.MatchWithWildCards("something", "som?t*g"));
        }
    }
}
