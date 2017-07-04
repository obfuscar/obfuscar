using Mono.Cecil;
using System;
using System.IO;
using System.Linq;

namespace Obfuscar.Helpers
{
    public static class AssemblyDefinitionExtensions
    {
        public static string GetPortableProfileDirectory(this AssemblyDefinition assembly)
        {
            foreach (var custom in assembly.CustomAttributes)
            {
                if (custom.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    if (!custom.HasProperties)
                        continue;
                    var framework = custom.Properties.First(property => property.Name == "FrameworkDisplayName");
                    var content = framework.Argument.Value.ToString();
                    if (!string.Equals(content, ".NET Portable Subset"))
                    {
                        return null;
                    }

                    var parts = custom.ConstructorArguments[0].Value.ToString().Split(',');
                    var root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    return Environment.ExpandEnvironmentVariables(
                        Path.Combine(
                            root,
                            "Reference Assemblies",
                            "Microsoft",
                            "Framework",
                            parts[0],
                            (parts[1].Split('='))[1],
                            "Profile",
                            (parts[2].Split('='))[1]));
                }
            }

            return null;
        }
    }
}
