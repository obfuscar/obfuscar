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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Obfuscar
{
    class NameGroup : IEnumerable<string>
    {
        HashSet<string> names = new HashSet<string>();

        public string GetNext()
        {
            int index = 0;
            string name;
            for (;;)
            {
                name = NameMaker.UniqueName(index++);
                if (!names.Contains(name))
                    return name;
            }
        }

        public bool Contains(string name)
        {
            return names.Contains(name);
        }

        public void AddAll(IEnumerable<string> range)
        {
            foreach (var item in range)
            {
                names.Add(item);
            }
        }

        public void Add(string name)
        {
            names.Add(name);
        }

        public void Remove(string name)
        {
            names.Remove(name);
        }

        public static string GetNext(IEnumerable<NameGroup> groups)
        {
            int index = 0;

            string name;
            for (;;)
            {
                name = NameMaker.UniqueName(index++);

                bool contained = false;
                foreach (NameGroup group in groups)
                {
                    if (group.Contains(name))
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained)
                    return name;
            }
        }

        /// <summary>
        /// See <see cref="IEnumerable.GetEnumerator"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return names.GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return names.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String s in names)
            {
                if (sb.Length != 0)
                    sb.Append(",");
                sb.Append(s);
            }
            return sb.ToString();
        }
    }
}
