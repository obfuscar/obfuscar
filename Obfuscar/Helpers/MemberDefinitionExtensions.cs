using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class MemberDefinitionExtensions
    {
        public static bool? MarkedToRename(this IMemberDefinition type, bool fromMember = false)
        {
#pragma warning disable 618
            var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
#pragma warning restore 618
            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

            foreach (CustomAttribute attr in type.CustomAttributes)
            {
                var attrFullName = attr.Constructor.DeclaringType.FullName;
                if (attrFullName == obfuscarObfuscate)
                    return (bool)(Helper.GetAttributePropertyByName(attr, "ShouldObfuscate") ?? true);

                if (attrFullName == reflectionObfuscate)
                {
                    var applyToMembers = (bool)(Helper.GetAttributePropertyByName(attr, "ApplyToMembers") ?? true);
                    var rename = !(bool)(Helper.GetAttributePropertyByName(attr, "Exclude") ?? true);

                    if (fromMember && !applyToMembers)
                        return !rename;

                    return rename;
                }
            }

            return type.DeclaringType == null ? null : MarkedToRename(type.DeclaringType, true);
        }

        public static void CleanAttributes(this IMemberDefinition type)
        {
            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

            for (int i = 0; i < type.CustomAttributes.Count; i++)
            {
                CustomAttribute attr = type.CustomAttributes[i];
                var attrFullName = attr.Constructor.DeclaringType.FullName;
                if (attrFullName == reflectionObfuscate)
                {
                    if ((bool)(Helper.GetAttributePropertyByName(attr, "StripAfterObfuscation") ?? true))
                    {
                        type.CustomAttributes.Remove(attr);
                    }
                }
            }
        }
    }
}
