using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
    public static class Helper
    {
        public static TypeDefinition Resolve(this InterfaceImplementation reference)
        {
            return reference.InterfaceType.Resolve();
        }
    }
}
