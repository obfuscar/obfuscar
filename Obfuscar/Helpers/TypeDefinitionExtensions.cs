using Mono.Cecil;
using System.Linq;

namespace Obfuscar.Helpers
{
	internal static class TypeDefinitionExtensions
	{
		static public bool IsTypePublic (this TypeDefinition type)
		{
			if (type.DeclaringType == null)
				return type.IsPublic;

			if (type.IsNestedFamily || type.IsNestedFamilyOrAssembly || type.IsNestedPublic)
				return IsTypePublic (type.DeclaringType);

			return false;
		}

		public static bool? MarkedToRename (this IMemberDefinition type, bool fromMember = false)
		{
			var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
			var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

			foreach (CustomAttribute attr in type.CustomAttributes) {
				var attrFullName = attr.Constructor.DeclaringType.FullName;
				if (attrFullName == obfuscarObfuscate)
					return (bool)(Helper.GetAttributePropertyByName (attr, "ShouldObfuscate") ?? true);

				if (attrFullName == reflectionObfuscate) {
					var applyToMembers = (bool)(Helper.GetAttributePropertyByName (attr, "ApplyToMembers") ?? true);
					var rename = !(bool)(Helper.GetAttributePropertyByName (attr, "Exclude") ?? true);

					if (fromMember && !applyToMembers)
						return !rename;

					return rename;
				}
			}

			return type.DeclaringType == null ? null : MarkedToRename (type.DeclaringType, true);
		}

		public static bool IsResourcesType (this TypeDefinition type)
		{
			var generated = type.CustomAttributes.FirstOrDefault (attribute => attribute.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute");
			if (generated == null)
				return false;

			var name = generated.ConstructorArguments [0].Value.ToString ();
			return name == "System.Resources.Tools.StronglyTypedResourceBuilder";
		}
	}
}
