using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscar
{
	internal static class TypeDefinitionExtensions
	{
		public static bool IsTruePublic (this TypeDefinition type)
		{
			if (!type.IsPublic)
				return false;

			var parentType = type.DeclaringType;
			while (parentType != null) {
				if (!parentType.IsPublic)
					return false;

				parentType = parentType.DeclaringType;
			}

			return true;
		}

		public static bool? MarkedToRename (this ICustomAttributeProvider type, bool member = false)
		{
			var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
			var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

			foreach (CustomAttribute attr in type.CustomAttributes) {
				var attrFullName = attr.Constructor.DeclaringType.FullName;
				if (attrFullName == obfuscarObfuscate)
					return (bool)(Helper.GetAttributePropertyByName (attr, "ShouldObfuscate") ?? true);

				if (attrFullName == reflectionObfuscate) {
					var applyToMembers = (bool)(Helper.GetAttributePropertyByName (attr, "ApplyToMembers") ?? true);
					var rename = !(bool)(Helper.GetAttributePropertyByName (attr, "Exclude") ?? false);

					if (member && !applyToMembers)
						return !rename;

					return rename;
				}
			}

			// no attribute found.
			return null;
		}
	}
}
