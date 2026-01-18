using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents an IL instruction in the mutable object model.
    /// This replaces Mono.Cecil.Cil.Instruction.
    /// </summary>
    public class MutableInstruction : IInstruction
    {
        /// <summary>
        /// Creates a new instruction with the specified opcode.
        /// </summary>
        public MutableInstruction(MutableOpCode opCode)
        {
            OpCode = opCode;
        }

        /// <summary>
        /// Creates a new instruction with the specified opcode and operand.
        /// </summary>
        public MutableInstruction(MutableOpCode opCode, object operand)
        {
            OpCode = opCode;
            Operand = operand;
        }

        /// <summary>
        /// The opcode of this instruction.
        /// </summary>
        public MutableOpCode OpCode { get; set; }

        /// <summary>
        /// The operand of this instruction. Can be:
        /// - null (for instructions with no operand)
        /// - int, long, float, double (for numeric literals)
        /// - string (for ldstr)
        /// - MutableInstruction (for branch targets)
        /// - MutableInstruction[] (for switch)
        /// - MutableMethodReference (for call/callvirt/newobj)
        /// - MutableFieldReference (for ldfld/stfld/ldsfld/stsfld)
        /// - MutableTypeReference (for newarr/castclass/isinst)
        /// - MutableVariableDefinition (for ldloc/stloc)
        /// - MutableParameterDefinition (for ldarg/starg)
        /// </summary>
        public object Operand { get; set; }

        /// <summary>
        /// The offset of this instruction in the method body.
        /// Computed during serialization.
        /// </summary>
        public int Offset { get; set; }

        int IInstruction.Offset => Offset;

        OpCode IInstruction.OpCode => MapOpCode(OpCode);

        object IInstruction.Operand => Operand;

        /// <summary>
        /// Gets the size of this instruction in bytes.
        /// </summary>
        public int GetSize()
        {
            int size = OpCode.Size;
            
            switch (OpCode.OperandType)
            {
                case MutableOperandType.InlineNone:
                    break;
                case MutableOperandType.ShortInlineBrTarget:
                case MutableOperandType.ShortInlineI:
                case MutableOperandType.ShortInlineVar:
                case MutableOperandType.ShortInlineArg:
                    size += 1;
                    break;
                case MutableOperandType.InlineVar:
                case MutableOperandType.InlineArg:
                    size += 2;
                    break;
                case MutableOperandType.InlineBrTarget:
                case MutableOperandType.InlineI:
                case MutableOperandType.ShortInlineR:
                case MutableOperandType.InlineString:
                case MutableOperandType.InlineMethod:
                case MutableOperandType.InlineField:
                case MutableOperandType.InlineType:
                case MutableOperandType.InlineTok:
                case MutableOperandType.InlineSig:
                    size += 4;
                    break;
                case MutableOperandType.InlineI8:
                case MutableOperandType.InlineR:
                    size += 8;
                    break;
                case MutableOperandType.InlineSwitch:
                    var targets = Operand as MutableInstruction[];
                    size += 4 + (targets?.Length ?? 0) * 4;
                    break;
            }
            
            return size;
        }

        /// <summary>
        /// Returns a string representation of this instruction.
        /// </summary>
        public override string ToString()
        {
            if (Operand == null)
                return $"IL_{Offset:X4}: {OpCode.Name}";
            return $"IL_{Offset:X4}: {OpCode.Name} {Operand}";
        }

        private static readonly Dictionary<short, OpCode> OpCodeByValue = BuildOpCodeMap();

        private static Dictionary<short, OpCode> BuildOpCodeMap()
        {
            var map = new Dictionary<short, OpCode>();
            var fields = typeof(OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.GetValue(null) is OpCode op)
                {
                    map[op.Value] = op;
                }
            }
            return map;
        }

        private static OpCode MapOpCode(MutableOpCode opCode)
        {
            if (OpCodeByValue.TryGetValue(opCode.Value, out var mapped))
            {
                return mapped;
            }

            return OpCodes.Nop;
        }
    }
}
