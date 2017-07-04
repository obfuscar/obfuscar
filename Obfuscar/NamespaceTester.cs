#region Copyright (c) 2008 Scherry Lemos. <scherrylemos@ig.com.br>

/// <copyright>
/// Copyright (c) 2008 Scherry Lemos<scherrylemos@ig.com.br>
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
    class NamespaceTester : IPredicate<string>
    {
        private readonly string name;
        private readonly Regex nameRx;

        public NamespaceTester(string name)
        {
            this.name = name;
        }

        public NamespaceTester(Regex nameRx)
        {
            this.nameRx = nameRx;
        }

        public bool Test(string ns, InheritMap map)
        {
            // regex matches
            if (nameRx != null && !nameRx.IsMatch(ns))
            {
                return false;
            }

            // name matches
            if (!string.IsNullOrEmpty(name) && !Helper.CompareOptionalRegex(ns, name))
            {
                return false;
            }

            return true;
        }
    }
}
