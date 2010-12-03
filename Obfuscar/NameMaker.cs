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

namespace Obfuscar
{
	static class NameMaker
	{
		const string uniqueChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
		const int numUniqueChars = 52;

		public static string UniqueName(int index)
		{
			return UniqueName(index, null);
		}

		public static string UniqueName(int index, string sep)
		{
			// optimization for simple case
			if (index < numUniqueChars)
				return uniqueChars[index].ToString();

			Stack<char> stack = new Stack<char>();

			do
			{
				stack.Push(uniqueChars[index % numUniqueChars]);
				index /= numUniqueChars;
			}
			while (index > 0);

			StringBuilder builder = new StringBuilder();
			builder.Append(stack.Pop());
			while (stack.Count > 0)
			{
				if (sep != null)
					builder.Append(sep);
				builder.Append(stack.Pop());
			}

			return builder.ToString();
		}

		public static string UniqueTypeName(int index)
		{
			return UniqueName(index % numUniqueChars, ".");
		}

		public static string UniqueNamespace(int index)
		{
			return UniqueName(index / numUniqueChars, ".");
		}
	}
}
