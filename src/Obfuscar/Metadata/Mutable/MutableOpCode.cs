using System.Collections.Generic;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents an IL opcode in the mutable object model.
    /// This replaces legacy Mono.Cecil.Cil.OpCode.
    /// </summary>
    public readonly struct MutableOpCode
    {
        /// <summary>
        /// Creates a new opcode.
        /// </summary>
        public MutableOpCode(string name, short value, MutableOperandType operandType, MutableFlowControl flowControl)
        {
            Name = name;
            Value = value;
            OperandType = operandType;
            FlowControl = flowControl;
        }

        /// <summary>
        /// The name of the opcode (e.g., "ldstr", "call").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The opcode value. Single-byte opcodes are 0x00-0xFF.
        /// Two-byte opcodes start with 0xFE.
        /// </summary>
        public short Value { get; }

        /// <summary>
        /// The type of operand this opcode takes.
        /// </summary>
        public MutableOperandType OperandType { get; }

        /// <summary>
        /// The flow control behavior of this opcode.
        /// </summary>
        public MutableFlowControl FlowControl { get; }

        /// <summary>
        /// Gets the size of this opcode in bytes (1 or 2).
        /// </summary>
        public int Size => Value > 0xFF || Value < 0 ? 2 : 1;

        /// <summary>
        /// Compares two opcodes for equality.
        /// </summary>
        public static bool operator ==(MutableOpCode a, MutableOpCode b) => a.Value == b.Value;

        /// <summary>
        /// Compares two opcodes for inequality.
        /// </summary>
        public static bool operator !=(MutableOpCode a, MutableOpCode b) => a.Value != b.Value;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MutableOpCode other && Value == other.Value;

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Name;
    }

    /// <summary>
    /// The type of operand an opcode takes.
    /// </summary>
    public enum MutableOperandType
    {
        /// <summary>No operand.</summary>
        InlineNone,
        /// <summary>32-bit branch target.</summary>
        InlineBrTarget,
        /// <summary>8-bit branch target.</summary>
        ShortInlineBrTarget,
        /// <summary>32-bit integer.</summary>
        InlineI,
        /// <summary>8-bit integer.</summary>
        ShortInlineI,
        /// <summary>64-bit integer.</summary>
        InlineI8,
        /// <summary>64-bit floating point.</summary>
        InlineR,
        /// <summary>32-bit floating point.</summary>
        ShortInlineR,
        /// <summary>String token.</summary>
        InlineString,
        /// <summary>Method token.</summary>
        InlineMethod,
        /// <summary>Field token.</summary>
        InlineField,
        /// <summary>Type token.</summary>
        InlineType,
        /// <summary>Generic token (type, method, or field).</summary>
        InlineTok,
        /// <summary>Signature token.</summary>
        InlineSig,
        /// <summary>Switch table.</summary>
        InlineSwitch,
        /// <summary>16-bit local variable index.</summary>
        InlineVar,
        /// <summary>8-bit local variable index.</summary>
        ShortInlineVar,
        /// <summary>16-bit argument index.</summary>
        InlineArg,
        /// <summary>8-bit argument index.</summary>
        ShortInlineArg,
    }

    /// <summary>
    /// Flow control behavior of an opcode.
    /// </summary>
    public enum MutableFlowControl
    {
        /// <summary>Normal sequential flow.</summary>
        Next,
        /// <summary>Unconditional branch.</summary>
        Branch,
        /// <summary>Conditional branch.</summary>
        Cond_Branch,
        /// <summary>Method call.</summary>
        Call,
        /// <summary>Return from method.</summary>
        Return,
        /// <summary>Throw exception.</summary>
        Throw,
        /// <summary>Breakpoint.</summary>
        Break,
        /// <summary>Metadata-only, not executed.</summary>
        Meta,
    }
}
