using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Obfuscar
{
	public static class AssemblyDefinitionExtensions
	{
		public static string GetPortableProfileDirectory (this AssemblyDefinition assembly)
		{
			foreach (var custom in assembly.CustomAttributes) {
				if (custom.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute") {
					var framework = custom.Properties [0].Argument.Value.ToString ();
					if (!string.Equals (framework, ".NET Portable Subset")) {
						return null;
					}

					var parts = custom.ConstructorArguments [0].Value.ToString ().Split (',');
					var root = Environment.ExpandEnvironmentVariables (
						                          Path.Combine (
							                          "%systemdrive%",
							                          "Program Files (x86)"));
					return Environment.ExpandEnvironmentVariables (
						Path.Combine (
							"%systemdrive%", 
							Directory.Exists (root) ? "Program Files (x86)" : "Program Files", 
							"Reference Assemblies", 
							"Microsoft", 
							"Framework",
							parts [0],
							(parts [1].Split ('=')) [1],
							"Profile",
							(parts [2].Split ('=')) [1])); 
				}
			}

			return null;
		}
	}
}