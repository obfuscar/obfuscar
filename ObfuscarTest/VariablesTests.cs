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

using Obfuscar;
using Xunit;

namespace ObfuscarTest
{
    public class VariablesTests
    {
        [Fact]
        public void CheckReplace()
        {
            Variables variables = new Variables();
            variables.Add("Key1", "Value1");

            string result = variables.Replace("This: $(Key1) got replaced.");
            Assert.Equal("This: Value1 got replaced.", result);
        }

        [Fact]
        public void CheckBadReplace()
        {
            Variables variables = new Variables();
            variables.Add("Key1", "Value1");
            var exception = Assert.Throws<ObfuscarException>(() => { variables.Replace("$(Unreplaceable)"); });
            Assert.Equal("Unable to replace variable:  Unreplaceable", exception.Message);
        }
    }
}
