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
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace ObfuscarTests
{
    [Collection("NameMaker")]
    public class SettingsTests
    {
        [Fact]
        public void CheckCannotObfuscateSigned()
        {
#if NETCOREAPP
            // IMPORANT: this is not not applicable for .NET Core
            return;
#endif
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='OptimizeMethods' value='false' />" +
                @"<Module file='$(InPath){2}WpfApplication1.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "WpfApplication1.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "WpfApplication1.dll"),
                    destFileName, true);
            }

            var obfuscator = TestHelper.Obfuscate(xml);

            Assert.False(obfuscator.Project.Settings.Optimize);
            Assert.Equal("AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz", NameMaker.UniqueChars);
        }

        [Fact]
        public void CheckUnicodeNames()
        {
#if NETCOREAPP
            // IMPORANT: this is not not applicable for .NET Core
            return;
#endif
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='OptimizeMethods' value='false' />" +
                @"<Var name='UseUnicodeNames' value='true' />" +
                @"<Module file='$(InPath){2}WpfApplication1.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "WpfApplication1.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "WpfApplication1.dll"),
                    destFileName, true);
            }

            var obfuscator = TestHelper.Obfuscate(xml);

            Assert.False(obfuscator.Project.Settings.Optimize);
            const string unicodeChars = "\u00A0\u1680" +
                            "\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u200B\u2010\u2011\u2012\u2013\u2014\u2015" +
                            "\u2022\u2024\u2025\u2027\u2028\u2029\u202A\u202B\u202C\u202D\u202E\u202F" +
                            "\u2032\u2035\u2033\u2036\u203E" +
                            "\u2047\u2048\u2049\u204A\u204B\u204C\u204D\u204E\u204F\u2050\u2051\u2052\u2053\u2054\u2055\u2056\u2057\u2058\u2059" +
                            "\u205A\u205B\u205C\u205D\u205E\u205F\u2060" +
                            "\u2061\u2062\u2063\u2064\u206A\u206B\u206C\u206D\u206E\u206F" +
                            "\u3000";
            Assert.Equal(unicodeChars, NameMaker.UniqueChars);
        }

        [Fact]
        public void CheckKoreanNames()
        {
#if NETCOREAPP
            // IMPORANT: this is not not applicable for .NET Core
            return;
#endif
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='OptimizeMethods' value='false' />" +
                @"<Var name='UseKoreanNames' value='true' />" +
                @"<Module file='$(InPath){2}WpfApplication1.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "WpfApplication1.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "WpfApplication1.dll"),
                    destFileName, true);
            }

            var obfuscator = TestHelper.Obfuscate(xml);

            Assert.False(obfuscator.Project.Settings.Optimize);
            Assert.Equal(NameMaker.KoreanChars, NameMaker.UniqueChars);
        }

        [Fact]
        public void CheckCustomNames()
        {
#if NETCOREAPP
            // IMPORANT: this is not not applicable for .NET Core
            return;
#endif
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='OptimizeMethods' value='false' />" +
                @"<Var name='CustomChars' value='abcdefghijklmn' />" +
                @"<Module file='$(InPath){2}WpfApplication1.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "WpfApplication1.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "WpfApplication1.dll"),
                    destFileName, true);
            }

            var obfuscator = TestHelper.Obfuscate(xml);

            Assert.False(obfuscator.Project.Settings.Optimize);
            Assert.Equal("abcdefghijklmn", NameMaker.UniqueChars);
        }

        [Fact]
        public void RejectsRelativePathsInConfig()
        {
            var xml = @"<?xml version='1.0'?><Obfuscator>" +
                      @"<Var name='InPath' value='Input' />" +
                      @"<Var name='OutPath' value='Output' />" +
                      @"</Obfuscator>";

            var ex = Assert.Throws<ObfuscarException>(() =>
            {
                var project = Project.FromXml(XDocument.Parse(xml), Directory.GetCurrentDirectory());
                _ = project.Settings;
            });

            Assert.Contains("InPath must be an absolute path", ex.Message);
        }

        [Fact]
        public void RejectsVariablePlaceholdersInConfig()
        {
            string tempOut = Path.Combine(Path.GetTempPath(), "obfuscar-tests");
            var xml = @"<?xml version='1.0'?><Obfuscator>" +
                      @"<Var name='InPath' value='$(InPath)' />" +
                      $@"<Var name='OutPath' value='{tempOut}' />" +
                      @"</Obfuscator>";

            var ex = Assert.Throws<ObfuscarException>(() =>
            {
                var project = Project.FromXml(XDocument.Parse(xml), Directory.GetCurrentDirectory());
                _ = project.Settings;
            });

            Assert.Contains("Variable substitution via $(...)", ex.Message);
        }

        [Fact]
        public void SkipSpecialNameDefaultsToFalse()
        {
            string tempRoot = Path.GetTempPath();
            string tempOut = Path.Combine(tempRoot, "obfuscar-tests");
            var xml = @"<?xml version='1.0'?><Obfuscator>" +
                      $@"<Var name='InPath' value='{tempRoot}' />" +
                      $@"<Var name='OutPath' value='{tempOut}' />" +
                      @"</Obfuscator>";

            var project = Project.FromXml(XDocument.Parse(xml), Directory.GetCurrentDirectory());
            Assert.False(project.Settings.SkipSpecialName);
        }

        [Fact]
        public void SkipSpecialNameCanBeEnabled()
        {
            string tempRoot = Path.GetTempPath();
            string tempOut = Path.Combine(tempRoot, "obfuscar-tests");
            var xml = @"<?xml version='1.0'?><Obfuscator>" +
                      $@"<Var name='InPath' value='{tempRoot}' />" +
                      $@"<Var name='OutPath' value='{tempOut}' />" +
                      @"<Var name='SkipSpecialName' value='true' />" +
                      @"</Obfuscator>";

            var project = Project.FromXml(XDocument.Parse(xml), Directory.GetCurrentDirectory());
            Assert.True(project.Settings.SkipSpecialName);
        }
    }
}
