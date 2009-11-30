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

using Mono.Cecil;

namespace Obfuscar
{
	class MethodTester : IPredicate<MethodKey>
	{
		private readonly MethodKey key;
		private readonly string name;
		private readonly Regex nameRx;
		private readonly string type;
		private readonly string attrib;

		public MethodTester( MethodKey key )
		{
			this.key = key;
		}

		public MethodTester( string name, string type, string attrib )
		{
			this.name = name;
			this.type = type;
			this.attrib = attrib;
		}

		public MethodTester( Regex nameRx, string type, string attrib )
		{
			this.nameRx = nameRx;
			this.type = type;
			this.attrib = attrib;
		}

		public bool Test( MethodKey method )
		{
			if ( key != null )
				return method == key;

			if ( Helper.CompareOptionalRegex(method.TypeKey.Fullname, type) && CheckMethodVisibility(this.attrib, method.MethodAttributes))
			{
				if ( name != null )
					return Helper.CompareOptionalRegex(method.Name, name);
				else
					return nameRx.IsMatch( method.Name );
			}

			return false;
		}

		static public bool CheckMethodVisibility(string attribute, MethodAttributes methodAttributes)
		{
			if (!string.IsNullOrEmpty(attribute))
			{
				MethodAttributes accessmask = (methodAttributes & MethodAttributes.MemberAccessMask);
				if (string.Equals(attribute, "public", StringComparison.CurrentCultureIgnoreCase))
				{
					if (accessmask == MethodAttributes.Public)
						return true;
				}
				else if (string.Equals(attribute, "protected", StringComparison.CurrentCultureIgnoreCase))
				{
					if (accessmask == MethodAttributes.Public || accessmask == MethodAttributes.Family)
						return true;
				}
				else
					throw new ApplicationException(string.Format("'{0}' is not valid for the 'attrib' value of skip elements. Only 'public' and 'protected' are supported by now.", attribute));
				
				// attrib value given, but the member is not public/protected. We signal that the Skip* rule should be ignored. The member is obfuscated in any case.
				return false;
			}
			else
				return true; // No attrib value given: The Skip* rule is processed normally.
		}
	}
}
