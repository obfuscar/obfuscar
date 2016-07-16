using Mono.Cecil;
using System;
using System.Linq;
using System.Runtime.Caching;

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

		private static CacheItemPolicy policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes (5) };

		public static bool IsResourcesType(this TypeDefinition type)
		{
			if (MemoryCache.Default.Contains (type.FullName))
				return (bool)MemoryCache.Default [type.FullName];

			var generated = type.CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute");
			var result = false;
			if (generated == null) {
				result = type.IsFormOrUserControl ();
			} else {
				var name = generated.ConstructorArguments [0].Value.ToString ();
				result = name == "System.Resources.Tools.StronglyTypedResourceBuilder";
			}

			MemoryCache.Default.Add (type.FullName, result, policy);
			return result;
		}

		private static bool IsFormOrUserControl (this TypeDefinition type)
		{
			if (type == null)
				return false;

			if (type.FullName == "System.Windows.Forms.Form" || type.FullName == "System.Windows.Forms.UserControl")
				return true;

			if (type.BaseType != null)
				return type.BaseType.Resolve().IsFormOrUserControl ();

			return false;
		}
	}
}
