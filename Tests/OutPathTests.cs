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
using System.IO;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class OutPathTests
    {
        private void CheckOutPath(string testPath)
        {
            var full = Environment.ExpandEnvironmentVariables(testPath);
            Assert.False(Directory.Exists(full), "Need a writeable temp path...wanted to create " + testPath);

            try
            {
                string xml = string.Format(
                    @"<?xml version='1.0'?>" +
                    @"<Obfuscator>" +
                    @"<Var name='OutPath' value='{0}' />" +
                    @"</Obfuscator>", testPath);

                Obfuscar.Obfuscator.CreateFromXml(xml);

                Assert.True(Directory.Exists(full), "Obfuscator should have created its missing OutPath.");
            }
            finally
            {
                // clean up...
                if (Directory.Exists(full))
                    Directory.Delete(full);
            }
        }

        [Fact]
        public void CheckCanCreateOutPath()
        {
            string testPath = Path.Combine(Path.GetTempPath(), "ObfuscarTestOutPath");

            CheckOutPath(testPath);
        }

        [Fact]
        public void CheckCanCreateOutPathWithEnvironmentVariables()
        {
            string testPath = Path.Combine("%temp%", "ObfuscarTestOutPath");

            CheckOutPath(testPath);
        }

        [Fact]
        public void CheckInvalidOutPath()
        {
            string testPath = Path.Combine(PathFailureTests.BadPath, "ObfuscarTestOutPath");
            Type t = Type.GetType("Mono.Runtime");
            if (t != null)
                return;

            var exception = Assert.Throws<ObfuscarException>(() => { CheckOutPath(testPath); });
            Assert.Equal("Could not create path specified by OutPath:  Q:\\Does\\Not\\Exist\\ObfuscarTestOutPath",
                exception.Message);
        }
    }
}
