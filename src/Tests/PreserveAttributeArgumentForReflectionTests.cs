using System.IO;
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class PreserveAttributeArgumentForReflectionTests
    {
        const string SampleAttributeFullName = "PreserveAttributeArgumentForReflectionTest.VisibleByAttribute";

        public string BuildAndObfuscateAssemblies()
        {
            var output = TestHelper.OutputPath;
            var name = "PreserveAttributeArgumentForReflectionTest";
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                string.Format("<Var name='CustomAttributesToPreservePropertyNames' value=' {0};\r\n{1};\r{2}' />",
                    SampleAttributeFullName,
                    "MyNamespace.Dummy1Attribute",
                    "MyNamespace.Dummy2Attribute"
                ) +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, output, Path.DirectorySeparatorChar, name);

            TestHelper.BuildAndObfuscate(name, string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            return Path.Combine(output, $"{name}.dll");
        }

        [Fact]
        public void CheckAttributeArgumentIsPreserved()
        {
            var output = BuildAndObfuscateAssemblies();
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(output);

            bool found = false;
            foreach (TypeDefinition typeDef in assmDef.MainModule.Types)
            {
                if (typeDef.Name == "<Module>" || typeDef.FullName == SampleAttributeFullName)
                    continue;                
                else
                    found = true;

                var firstProperty = typeDef.Properties.FirstOrDefault(x => x.PropertyType.FullName == "System.Boolean");
                var secondProperty = typeDef.Properties.FirstOrDefault(x => x.PropertyType.FullName == "System.String");

                // both properties must exist
                Assert.NotNull(firstProperty); 
                Assert.NotNull(secondProperty);

                // both properties are obfuscated
                Assert.NotEqual("FirstProperty", firstProperty.Name);
                Assert.NotEqual("SecondProperty", secondProperty.Name);

                // second property should have CategoryAttribute
                CustomAttribute attr = secondProperty.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == SampleAttributeFullName);
                Assert.NotNull(attr);

                Assert.True(attr.ConstructorArguments.Count > 0); // "VisibleByAttribute should have parameters.");

                // "VisibleByAttribute param should be an obfuscated name of first property.");
                Assert.Equal(firstProperty.Name, attr.ConstructorArguments[0].Value);                
            }

            Assert.True(found, "Should have found non-<Module> type.");
        }
    }
}
