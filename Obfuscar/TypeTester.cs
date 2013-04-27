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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

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
		private readonly Regex nameRx;
		private readonly TypeSkipFlags skipFlags;
		private readonly string attrib;
		private readonly string inherits;
		private readonly bool? isStatic;
		[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification = "Reviewed. Suppression is OK here.")]
		private readonly bool?
			isSerializable;

		public TypeSkipFlags SkipFlags {
			get { return this.skipFlags; }
		}

		public TypeTester (string name)
            : this(name, TypeSkipFlags.SkipNone, string.Empty)
		{
		}

		public TypeTester (string name, TypeSkipFlags skipFlags, string attrib)
		{
			this.name = name;
			this.skipFlags = skipFlags;
			this.attrib = attrib.ToLower ();
		}

		public TypeTester (string name, TypeSkipFlags skipFlags, string attrib, string inherits, bool? isStatic, bool? isSeriablizable) 
            : this(name,skipFlags, attrib)
		{
			this.inherits = inherits;
			this.isStatic = isStatic;
			this.isSerializable = isSeriablizable;
		}

		public TypeTester (Regex nameRx, TypeSkipFlags skipFlags, string attrib)
		{
			this.nameRx = nameRx;
			this.skipFlags = skipFlags;
			this.attrib = attrib.ToLower ();
		}
        
		public TypeTester (Regex nameRx, TypeSkipFlags skipFlags, string attrib, string inherits, bool? isStatic, bool? isSeriablizable) 
            : this(nameRx,skipFlags, attrib)
		{
			this.inherits = inherits;
			this.isStatic = isStatic;
			this.isSerializable = isSeriablizable;
		}

		public bool Test (TypeKey type, InheritMap map)
		{
			if (!string.IsNullOrEmpty (attrib)) {
				if (string.Equals (attrib, "public", StringComparison.InvariantCultureIgnoreCase)) {
					if (!MethodTester.IsTypePublic (type.TypeDefinition)) {
						return false;
					}
				} else {
					throw new ApplicationException (string.Format ("'{0}' is not valid for the 'attrib' value of the SkipType element. Only 'public' is supported by now.", attrib));
				}
			}

			// type's regex matches
			if (nameRx != null && !nameRx.IsMatch (type.Fullname)) {
				return false;
			}
            
			// type's name matches
			if (!string.IsNullOrEmpty (name) && !Helper.CompareOptionalRegex (type.Fullname, name)) {
				return false;
			}

			if (isSerializable.HasValue) {
				if (isSerializable != type.TypeDefinition.IsSerializable) {
					return false;
				}
			}

			if (isStatic.HasValue) {
				if (isStatic != (type.TypeDefinition.IsSealed && type.TypeDefinition.IsAbstract)) {
					return false;
				}
			}

			if (!string.IsNullOrEmpty (inherits)) {
				if (!map.Inherits (type.TypeDefinition, inherits)) {
					return false;
				}
			}

			return true;
		}
	}
}
