using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTest
{
    public class AttributeWithGenericTypeParamTest
    {
        public string BuildAndObfuscate()
        {
            string outputPath = TestHelper.OutputPath;
            var nameA = "AssemblyA";
            var name = "AttributeWithGenericTypeParam";


            var provider = new Microsoft.CSharp.CSharpCodeProvider();
            var cp = new CompilerParameters();
            cp.GenerateExecutable = false;
            cp.GenerateInMemory = false;
            cp.TreatWarningsAsErrors = true;

            string assemblyAPath = Path.Combine(TestHelper.InputPath, nameA + ".dll");
            cp.OutputAssembly = assemblyAPath;
            var cr = provider.CompileAssemblyFromFile(cp, Path.Combine(TestHelper.InputPath, nameA + ".cs"));
            if (cr.Errors.Count > 0)
                Assert.True(false, $"Unable to compile test assembly: {nameA}, {cr.Errors[0].ErrorText}");

            cp.ReferencedAssemblies.Add(assemblyAPath);
            cp.OutputAssembly = Path.Combine(TestHelper.InputPath, name + ".dll");
            cr = provider.CompileAssemblyFromFile(cp, Path.Combine(TestHelper.InputPath, name + ".cs"));
            if (cr.Errors.Count > 0)
                Assert.True(false, $"Unable to compile test assembly:  {name}, {cr.Errors[0].ErrorText}");

            var xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{4}.dll' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, name, nameA);

            TestHelper.Obfuscate(xml);
            return Path.Combine(outputPath, $"{name}.dll");
        }

        [Fact]
        public void CheckAttributesWithGenericTypes()
        {
            var output = BuildAndObfuscate();
            var assmDef = AssemblyDefinition.ReadAssembly(output);

            Assert.Equal(2+1, assmDef.MainModule.Types.Count);

            var attr = assmDef.MainModule.Types
                .SelectMany(x => x.CustomAttributes)
                .SelectMany(x => x.ConstructorArguments)
                .Select(x => x.Value)
                .OfType<GenericInstanceType>()
                // expect only 1 attribute param like List<ClassA>
                .Single();

            Assert.Contains("List", attr.FullName);
            Assert.False(attr.FullName.Contains("ClassA"), "Reference to the ClassA has not been updated");
        }
    }
}
