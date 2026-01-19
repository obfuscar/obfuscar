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
using System.Threading.Tasks;

namespace TestClasses
{
	public class PublicClass1
	{
		private string test = "test1"; // hidden 1

		public string PublicMethod()
		{
			return "class1" + test; // hidden 2
		}
	}

	public class PublicClass2
	{
		private string test = "test2"; // hidden 3

		public string Test()
		{
			return "class2" + test; // hidden 4
		}
	}

    public class PublicClass3
    {
        private string test = ""; // hidden 5

        public string Test()
        {
            return "class3" + test; // hidden 6
        }
    }

    public class PublicClass4
    {
        private string test = string.Empty; // not hidden

        public string Test()
        {
            return "class4" + test; // hidden 7
        }

        public async Task<string> TestAsync()
        {
            return await Task.FromResult("class4-async" + test); // hidden 8
        }

        public Task<string> TestAsync2()
        {
            string fff = "fsdfsdfd"; // hidden 9
            return null;
        }

        public async void TestAsync3()
        {
            string fff = "fsdfsdfd333"; // hidden 10
            return;
        }
    }
}
