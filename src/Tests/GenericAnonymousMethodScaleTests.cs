using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ObfuscarTests
{
    /// <summary>
    /// Scale-oriented reproducer for #607. The issue reporter observed that
    /// obfuscation of anonymous methods depends on the total number of processed
    /// methods, so this generates many classes/methods with lambdas (including
    /// lambdas in generic methods that produce generic &lt;&gt;c__N&lt;T&gt; types)
    /// and validates the obfuscated assembly.
    /// </summary>
    public class GenericAnonymousMethodScaleTests
    {
        private static string GenerateSource(int classCount, int methodsPerClass)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace ScaleGen");
            sb.AppendLine("{");
            sb.AppendLine("    public static class EntryPoint");
            sb.AppendLine("    {");
            sb.AppendLine("        public static string Test()");
            sb.AppendLine("        {");

            for (int c = 0; c < classCount; c++)
            {
                sb.AppendLine($"            new C{c}().Run();");
            }

            sb.AppendLine("            return \"ok\";");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            for (int c = 0; c < classCount; c++)
            {
                sb.AppendLine($"    public class C{c}");
                sb.AppendLine("    {");
                sb.AppendLine("        public void Run()");
                sb.AppendLine("        {");
                sb.AppendLine("            RunGeneric(new List<string> { \"a\" });");
                sb.AppendLine("        }");
                sb.AppendLine("        public void RunGeneric<T>(IEnumerable<T> items) where T : class");
                sb.AppendLine("        {");
                sb.AppendLine("            Func<T, bool> outer = x => x != null;");
                sb.AppendLine("            foreach (var i in items) { if (outer(i)) { } }");
                sb.AppendLine("        }");

                for (int m = 0; m < methodsPerClass; m++)
                {
                    sb.AppendLine($"        public void M{m}<T>(IEnumerable<T> items) where T : class");
                    sb.AppendLine("        {");
                    sb.AppendLine("            Func<T, bool> f = x => x != null;");
                    sb.AppendLine("            Func<T, T> g = x => x;");
                    sb.AppendLine("            foreach (var i in items) { if (f(i) != null) { g(i); } }");
                    sb.AppendLine("        }");
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void BuildAndVerify(int classCount, int methodsPerClass, OptimizationLevel optimizationLevel,
            string extraVars = "")
        {
            string name = $"ScaleGen_{classCount}_{methodsPerClass}";
            string sourcePath = Path.Combine(TestHelper.InputPath, name + ".cs");
            File.WriteAllText(sourcePath, GenerateSource(classCount, methodsPerClass));

            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"{4}" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, name, extraVars);

            TestHelper.BuildAndObfuscate(
                name,
                string.Empty,
                xml,
                hideStrings: true,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10,
                useNetFramework: false,
                optimizationLevel: optimizationLevel);

            string assemblyPath = Path.Combine(outputPath, name + ".dll");
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load obfuscated assembly [{classCount}x{methodsPerClass}]: {ex.GetType().Name}: {ex.Message}", ex);
            }

            var types = assembly.GetTypes();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                Assert.True(seen.Add(type.FullName!), $"Duplicate type full name: {type.FullName}");
            }

            foreach (var type in types)
            {
                var method = type.GetMethod("Test", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, Array.Empty<object>());
                    Assert.Equal("ok", result);
                }
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(4, 3)]
        [InlineData(8, 4)]
        [InlineData(16, 5)]
        [InlineData(32, 8)]
        public void CheckScale_Debug(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Debug);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(4, 3)]
        [InlineData(16, 5)]
        [InlineData(32, 8)]
        public void CheckScale_Release(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Release);
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(16, 5)]
        [InlineData(32, 8)]
        public void CheckScale_SkipGenerated(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Debug,
                @"<Var name='SkipGenerated' value='true' />");
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(16, 5)]
        [InlineData(32, 8)]
        public void CheckScale_KeepPublicApi(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Debug,
                @"<Var name='KeepPublicApi' value='true' />");
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(16, 5)]
        public void CheckScale_KoreanNames(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Debug,
                @"<Var name='UseKoreanNames' value='true' />");
        }

        [Theory]
        [InlineData(4, 3)]
        [InlineData(16, 5)]
        public void CheckScale_ReuseNamesFalse(int classCount, int methodsPerClass)
        {
            BuildAndVerify(classCount, methodsPerClass, OptimizationLevel.Debug,
                @"<Var name='ReuseNames' value='false' />");
        }
    }
}
