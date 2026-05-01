using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class DictionaryKeyValuePairAccessTests
    {
        private const string AssemblyName = "AssemblyWithDictionaryKeyValuePairAccess";
        private const string EntryTypeName = "TestClasses.MyDictionary";

        private static string InvokeTest(string assemblyPath)
        {
            Assembly assembly = Assembly.LoadFile(assemblyPath);
            Type entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            MethodInfo test = entryType.GetMethod("Test", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(test);
            return (string)test.Invoke(null, Array.Empty<object>());
        }

        private static void WriteInputSource()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

namespace TestClasses
{
    public class MyDictionary : Dictionary<string, string>
    {
        public MyDictionary AddPair(KeyValuePair<string, string> pair)
        {
            Add(pair.Key, pair.Value);
            return this;
        }

        public static string Test()
        {
            var dict = new MyDictionary();
            dict.Add(""a"", ""b"");
            dict.AddPair(new KeyValuePair<string, string>(""c"", ""d""));

            string allKeys = string.Join("","", dict.Select(x => x.Key).OrderBy(x => x).ToArray());
            string firstKey = dict.First().Key;
            string firstValue = dict.First().Value;

            return allKeys + ""|"" + firstKey + ""|"" + firstValue + ""|"" + dict.Count;
        }
    }
}";

            File.WriteAllText(Path.Combine(TestHelper.InputPath, AssemblyName + ".cs"), source);
        }

        private static Obfuscator BuildAndObfuscate(bool hideStrings)
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='{3}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar,
                hideStrings ? "true" : "false");

            TestHelper.CleanInput();
            WriteInputSource();
            TestHelper.BuildAssembly(AssemblyName, string.Empty, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            return TestHelper.Obfuscate(xml);
        }

        [Fact]
        public void CheckDictionaryKeyValuePairAccessBaseline()
        {
            TestHelper.CleanInput();
            WriteInputSource();
            TestHelper.BuildAssembly(AssemblyName, string.Empty, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string inputAssemblyPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            string result = InvokeTest(inputAssemblyPath);
            Assert.Equal("a,c|a|b|2", result);
        }

        [Fact]
        public void CheckDictionaryKeyValuePairAccessAfterObfuscationWithHideStrings()
        {
            Obfuscator item = BuildAndObfuscate(hideStrings: true);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            string result = InvokeTest(outputPath);
            Assert.Equal("a,c|a|b|2", result);
        }
    }
}
