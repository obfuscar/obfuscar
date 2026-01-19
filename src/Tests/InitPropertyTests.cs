using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ObfuscarTests
{
    public class InitPropertyTests
    {
        [Fact]
        public void InitOnlyPropertyIsNotConvertedToSet()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>
                <Obfuscator>
                    <Var name='InPath' value='{0}' />
                    <Var name='OutPath' value='{1}' />
                    <Module file='$(InPath){2}InitProperty.dll' />
                </Obfuscator>",
                TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("InitProperty", string.Empty, xml, false,
                Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9, useNetFramework: false);

            var originalPath = Path.Combine(TestHelper.InputPath, "InitProperty.dll");
            var obfuscatedPath = Path.Combine(outputPath, "InitProperty.dll");

            var origFound = HasInitModifier(originalPath, out var origModifier);
            var obfFound = HasInitModifier(obfuscatedPath, out var obfModifier);
            
            Assert.True(origFound, $"Original should have modreq(IsExternalInit), found: {origModifier}");
            Assert.True(obfFound, $"Obfuscated should preserve modreq(IsExternalInit). Original={origModifier}, Obfuscated={obfModifier}");
        }

        private static bool HasInitModifier(string assemblyPath, out string foundModifier)
        {
            foundModifier = "none";
            using var stream = File.OpenRead(assemblyPath);
            using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader();

            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);
                if (reader.GetString(typeDef.Namespace) != "TestInit" ||
                    reader.GetString(typeDef.Name) != "InitOnlyClass")
                    continue;

                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var method = reader.GetMethodDefinition(methodHandle);
                    if (reader.GetString(method.Name) != "set_Number")
                        continue;

                    var detector = new InitModifierDetector();
                    method.DecodeSignature(detector, reader);
                    foundModifier = detector.FoundModifier ?? "none";
                    return detector.Found;
                }
            }
            return false;
        }

        /// <summary>
        /// Minimal type provider that detects modreq(IsExternalInit) on method parameters.
        /// </summary>
        private sealed class InitModifierDetector : ISignatureTypeProvider<string, MetadataReader>
        {
            public bool Found { get; private set; }
            public string FoundModifier { get; private set; }

            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
            {
                FoundModifier = $"modreq={isRequired}, modifier={modifier}";
                if (isRequired && modifier == "System.Runtime.CompilerServices.IsExternalInit")
                    Found = true;
                return unmodifiedType;
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var tr = reader.GetTypeReference(handle);
                return reader.GetString(tr.Namespace) + "." + reader.GetString(tr.Name);
            }

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var td = reader.GetTypeDefinition(handle);
                return reader.GetString(td.Namespace) + "." + reader.GetString(td.Name);
            }

            // Remaining methods - we only care about type names for modifier detection
            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
            public string GetTypeFromSpecification(MetadataReader reader, MetadataReader ctx, TypeSpecificationHandle h, byte k) => "";
            public string GetSZArrayType(string elementType) => elementType;
            public string GetArrayType(string elementType, ArrayShape shape) => elementType;
            public string GetByReferenceType(string elementType) => elementType;
            public string GetPointerType(string elementType) => elementType;
            public string GetPinnedType(string elementType) => elementType;
            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArgs) => genericType;
            public string GetGenericMethodParameter(MetadataReader ctx, int index) => "";
            public string GetGenericTypeParameter(MetadataReader ctx, int index) => "";
            public string GetFunctionPointerType(MethodSignature<string> signature) => "";
        }
    }
}
