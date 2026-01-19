using System.Collections.Generic;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a method body containing IL instructions and local variables.
    /// This replaces legacy Mono.Cecil.Cil.MethodBody.
    /// </summary>
    public class MutableMethodBody : IMethodBody
    {
        /// <summary>
        /// Creates a new method body for the specified method.
        /// </summary>
        public MutableMethodBody(MutableMethodDefinition method)
        {
            Method = method;
            Instructions = new List<MutableInstruction>();
            Variables = new List<MutableVariableDefinition>();
            ExceptionHandlers = new List<MutableExceptionHandler>();
        }

        /// <summary>
        /// The method that owns this body.
        /// </summary>
        public MutableMethodDefinition Method { get; }

        /// <summary>
        /// The list of IL instructions.
        /// </summary>
        public List<MutableInstruction> Instructions { get; }

        /// <summary>
        /// The list of local variables.
        /// </summary>
        public List<MutableVariableDefinition> Variables { get; }

        /// <summary>
        /// The list of exception handlers.
        /// </summary>
        public List<MutableExceptionHandler> ExceptionHandlers { get; }

        /// <summary>
        /// Maximum stack size. If not set, will be computed automatically.
        /// </summary>
        public int MaxStackSize { get; set; } = 8;

        /// <summary>
        /// Whether to initialize local variables to their default values.
        /// </summary>
        public bool InitLocals { get; set; } = true;

        IEnumerable<IInstruction> IMethodBody.Instructions => Instructions;

        IEnumerable<IExceptionHandler> IMethodBody.ExceptionHandlers => ExceptionHandlers;

        IEnumerable<IVariableDefinition> IMethodBody.Variables => Variables;

        /// <summary>
        /// Computes instruction offsets for all instructions in the body.
        /// </summary>
        public void ComputeOffsets()
        {
            int offset = 0;
            foreach (var instruction in Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }

        /// <summary>
        /// Gets the code size in bytes.
        /// </summary>
        public int CodeSize
        {
            get
            {
                int size = 0;
                foreach (var instruction in Instructions)
                {
                    size += instruction.GetSize();
                }
                return size;
            }
        }
    }

    /// <summary>
    /// Represents a local variable in a method body.
    /// This replaces legacy Mono.Cecil.Cil.VariableDefinition.
    /// </summary>
    public class MutableVariableDefinition : IVariableDefinition
    {
        /// <summary>
        /// Creates a new local variable.
        /// </summary>
        public MutableVariableDefinition(MutableTypeReference variableType)
        {
            VariableType = variableType;
        }

        /// <summary>
        /// The type of the local variable.
        /// </summary>
        public MutableTypeReference VariableType { get; set; }

        /// <summary>
        /// The index of this variable in the method's local variable list.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Optional name for debugging.
        /// </summary>
        public string Name { get; set; }

        public string VariableTypeName => VariableType?.FullName ?? string.Empty;

        public bool IsPinned { get; set; }
    }

    /// <summary>
    /// Represents an exception handler in a method body.
    /// This replaces legacy Mono.Cecil.Cil.ExceptionHandler.
    /// </summary>
    public class MutableExceptionHandler : IExceptionHandler
    {
        /// <summary>
        /// The type of exception handler.
        /// </summary>
        public MutableExceptionHandlerType HandlerType { get; set; }

        /// <summary>
        /// The first instruction of the try block.
        /// </summary>
        public MutableInstruction TryStart { get; set; }

        /// <summary>
        /// The first instruction after the try block.
        /// </summary>
        public MutableInstruction TryEnd { get; set; }

        /// <summary>
        /// The first instruction of the handler block.
        /// </summary>
        public MutableInstruction HandlerStart { get; set; }

        /// <summary>
        /// The first instruction after the handler block.
        /// </summary>
        public MutableInstruction HandlerEnd { get; set; }

        /// <summary>
        /// For catch handlers, the type of exception to catch.
        /// </summary>
        public MutableTypeReference CatchType { get; set; }

        /// <summary>
        /// For filter handlers, the first instruction of the filter block.
        /// </summary>
        public MutableInstruction FilterStart { get; set; }

        ExceptionHandlerType IExceptionHandler.HandlerType => MapHandlerType(HandlerType);

        int IExceptionHandler.TryStart => TryStart?.Offset ?? 0;

        int IExceptionHandler.TryEnd => TryEnd?.Offset ?? 0;

        int IExceptionHandler.HandlerStart => HandlerStart?.Offset ?? 0;

        int IExceptionHandler.HandlerEnd => HandlerEnd?.Offset ?? 0;

        string IExceptionHandler.CatchTypeName => CatchType?.FullName ?? string.Empty;

        int IExceptionHandler.FilterStart => FilterStart?.Offset ?? 0;

        private static ExceptionHandlerType MapHandlerType(MutableExceptionHandlerType handlerType)
        {
            switch (handlerType)
            {
                case MutableExceptionHandlerType.Filter:
                    return ExceptionHandlerType.Filter;
                case MutableExceptionHandlerType.Finally:
                    return ExceptionHandlerType.Finally;
                case MutableExceptionHandlerType.Fault:
                    return ExceptionHandlerType.Fault;
                case MutableExceptionHandlerType.Catch:
                default:
                    return ExceptionHandlerType.Catch;
            }
        }
    }

    /// <summary>
    /// The type of exception handler.
    /// </summary>
    public enum MutableExceptionHandlerType
    {
        /// <summary>A typed exception handler.</summary>
        Catch = 0,
        /// <summary>A filter-based exception handler.</summary>
        Filter = 1,
        /// <summary>A finally block.</summary>
        Finally = 2,
        /// <summary>A fault block (like finally but only on exception).</summary>
        Fault = 4,
    }
}
