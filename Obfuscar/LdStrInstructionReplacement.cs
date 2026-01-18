using Obfuscar.Metadata.Mutable;

namespace Obfuscar
{
    /// <summary>
    /// Represents a replacement mapping for ldstr instructions during string obfuscation.
    /// </summary>
    /// <remarks>
    /// This class uses the mutable SRM-backed instruction model for IL manipulation.
    /// The modified assembly is later written using SrmAssemblyWriter.
    /// </remarks>
    internal class LdStrInstructionReplacement
    {
        public LdStrInstructionReplacement(int oldIndex, MutableInstruction newinstruction)
        {
            this.OldIndex = oldIndex;
            NewInstruction = newinstruction;
        }

        /// <summary>
        /// The index of the original ldstr instruction in the method body.
        /// </summary>
        public int OldIndex { get; }
        
        /// <summary>
        /// The new Call instruction that replaces the ldstr.
        /// </summary>
        public MutableInstruction NewInstruction { get; }
    }
}
