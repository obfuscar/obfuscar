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

using Mono.Cecil;

namespace Obfuscar
{
	[Flags]
	public enum TypeSkipFlags
	{
		SkipNone = 0x00,
		SkipEvent = 0x01,
		SkipProperty = 0x02,
		SkipField = 0x04,
		SkipMethod = 0x08,
		SkipStringHiding = 0x10,
	}

	class TypeTester : IPredicate<TypeKey>
	{
		private readonly string name;
		private readonly TypeSkipFlags skipFlags;

		public TypeSkipFlags SkipFlags
		{
			get { return this.skipFlags; }
		}

		public TypeTester( string name )
			: this( name, TypeSkipFlags.SkipNone )
		{
		}

		public TypeTester( string name, TypeSkipFlags skipFlags )
		{
			this.name = name;
			this.skipFlags = skipFlags;
		}

		public bool Test( TypeKey type )
		{
			return Helper.CompareOptionalRegex(type.Fullname, name);
		}
	}
}
