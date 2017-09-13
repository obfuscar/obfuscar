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
    /// <summary>
    /// Attribute used to mark whether a type should be obfuscated.
    /// </summary>
    [Obsolete(
        "Please use ObfuscationAttribute of .NET Framework, http://msdn.microsoft.com/en-us/library/system.reflection.obfuscationattribute(v=vs.110).aspx")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Interface | AttributeTargets.Enum)]
    public class ObfuscateAttribute : Attribute
    {
        /// <summary>
        /// Marks a type to be obfuscated.
        /// </summary>
        public ObfuscateAttribute()
            : this(true)
        {
        }

        /// <summary>
        /// Marks whether a type should be obfuscated.
        /// </summary>
        public ObfuscateAttribute(bool shouldObfuscate)
        {
            this.ShouldObfuscate = shouldObfuscate;
        }

        /// <summary>
        /// Specifies whether the marked type should or should not be obfuscated.  Defaults to
        /// see <see langref="true"/>.
        /// </summary>
        public bool ShouldObfuscate { get; set; }
    }
}
