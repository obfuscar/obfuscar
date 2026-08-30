using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Obfuscar;
using Obfuscar.Helpers;
using Xunit;

namespace ObfuscarTests
{
    public class DiagnosticTests
    {
        [Fact]
        public void DiagnoseEnumerableParameterBug()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}EnumerableParameterIsSetToNullTest.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "EnumerableParameterIsSetToNullTest",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string outputAssemblyPath = Path.Combine(outputPath, "EnumerableParameterIsSetToNullTest.dll");
            var logPath = Path.Combine(Path.GetTempPath(), "obfuscar-diagnostic.log");
            using (var log = new StreamWriter(logPath, false))
            {
                var assem = AssemblyDefinition.ReadAssembly(outputAssemblyPath);
                foreach (var type in assem.MainModule.Types)
                {
                    DumpType(log, type, 0);
                }

                void DumpType(StreamWriter w, TypeDefinition td, int indent)
                {
                    var prefix = new string(' ', indent * 2);
                    w.WriteLine($"{prefix}Type: {td.FullName} (attrs={td.Attributes})");
                    foreach (var field in td.Fields)
                        w.WriteLine($"{prefix}  Field: \"{field.Name}\" ({field.FieldType.GetFullName()})");
                    foreach (var method in td.Methods)
                    {
                        w.WriteLine($"{prefix}  Method: \"{method.Name}\"");
                        if (method.Body != null)
                        {
                            foreach (var inst in method.Body.Instructions)
                            {
                                if (inst.Operand is FieldReference fr)
                                    w.WriteLine($"{prefix}    {inst.OpCode} \"{fr.Name}\" on \"{fr.DeclaringType?.GetFullName()}\"");
                                else if (inst.Operand is MethodReference mr)
                                    w.WriteLine($"{prefix}    {inst.OpCode} \"{mr.Name}\" on \"{mr.DeclaringType?.GetFullName()}\"");
                            }
                        }
                    }
                    foreach (var nested in td.NestedTypes)
                        DumpType(w, nested, indent + 1);
                }
            }
        }
    }
}
