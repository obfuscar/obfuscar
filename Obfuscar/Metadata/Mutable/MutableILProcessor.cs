using System.Collections.Generic;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Helper class for emitting IL instructions into a method body.
    /// This replaces legacy Mono.Cecil.Cil.ILProcessor.
    /// </summary>
    public class MutableILProcessor
    {
        /// <summary>
        /// Creates an IL processor for the specified method body.
        /// </summary>
        public MutableILProcessor(MutableMethodBody body)
        {
            Body = body;
        }

        /// <summary>
        /// The method body being modified.
        /// </summary>
        public MutableMethodBody Body { get; }

        /// <summary>
        /// Creates a new instruction with no operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode)
        {
            return new MutableInstruction(opCode);
        }

        /// <summary>
        /// Creates a new instruction with an integer operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, int value)
        {
            return new MutableInstruction(opCode, value);
        }

        /// <summary>
        /// Creates a new instruction with a long operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, long value)
        {
            return new MutableInstruction(opCode, value);
        }

        /// <summary>
        /// Creates a new instruction with a float operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, float value)
        {
            return new MutableInstruction(opCode, value);
        }

        /// <summary>
        /// Creates a new instruction with a double operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, double value)
        {
            return new MutableInstruction(opCode, value);
        }

        /// <summary>
        /// Creates a new instruction with a string operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, string value)
        {
            return new MutableInstruction(opCode, value);
        }

        /// <summary>
        /// Creates a new instruction with a method reference operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableMethodReference method)
        {
            return new MutableInstruction(opCode, method);
        }

        /// <summary>
        /// Creates a new instruction with a field reference operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableFieldReference field)
        {
            return new MutableInstruction(opCode, field);
        }

        /// <summary>
        /// Creates a new instruction with a type reference operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableTypeReference type)
        {
            return new MutableInstruction(opCode, type);
        }

        /// <summary>
        /// Creates a new instruction with a branch target operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableInstruction target)
        {
            return new MutableInstruction(opCode, target);
        }

        /// <summary>
        /// Creates a new instruction with a local variable operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableVariableDefinition variable)
        {
            return new MutableInstruction(opCode, variable);
        }

        /// <summary>
        /// Creates a new instruction with a parameter operand.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableParameterDefinition parameter)
        {
            return new MutableInstruction(opCode, parameter);
        }

        /// <summary>
        /// Creates a new instruction with switch targets.
        /// </summary>
        public MutableInstruction Create(MutableOpCode opCode, MutableInstruction[] targets)
        {
            return new MutableInstruction(opCode, targets);
        }

        /// <summary>
        /// Appends an instruction to the end of the method body.
        /// </summary>
        public void Append(MutableInstruction instruction)
        {
            Body.Instructions.Add(instruction);
        }

        /// <summary>
        /// Emits an instruction with no operand.
        /// </summary>
        public void Emit(MutableOpCode opCode)
        {
            Append(Create(opCode));
        }

        /// <summary>
        /// Emits an instruction with an integer operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, int value)
        {
            Append(Create(opCode, value));
        }

        /// <summary>
        /// Emits an instruction with a long operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, long value)
        {
            Append(Create(opCode, value));
        }

        /// <summary>
        /// Emits an instruction with a float operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, float value)
        {
            Append(Create(opCode, value));
        }

        /// <summary>
        /// Emits an instruction with a double operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, double value)
        {
            Append(Create(opCode, value));
        }

        /// <summary>
        /// Emits an instruction with a string operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, string value)
        {
            Append(Create(opCode, value));
        }

        /// <summary>
        /// Emits an instruction with a method reference operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableMethodReference method)
        {
            Append(Create(opCode, method));
        }

        /// <summary>
        /// Emits an instruction with a field reference operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableFieldReference field)
        {
            Append(Create(opCode, field));
        }

        /// <summary>
        /// Emits an instruction with a type reference operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableTypeReference type)
        {
            Append(Create(opCode, type));
        }

        /// <summary>
        /// Emits an instruction with a branch target operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableInstruction target)
        {
            Append(Create(opCode, target));
        }

        /// <summary>
        /// Emits an instruction with a local variable operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableVariableDefinition variable)
        {
            Append(Create(opCode, variable));
        }

        /// <summary>
        /// Emits an instruction with a parameter operand.
        /// </summary>
        public void Emit(MutableOpCode opCode, MutableParameterDefinition parameter)
        {
            Append(Create(opCode, parameter));
        }

        /// <summary>
        /// Inserts an instruction before another instruction.
        /// </summary>
        public void InsertBefore(MutableInstruction target, MutableInstruction instruction)
        {
            int index = Body.Instructions.IndexOf(target);
            if (index >= 0)
            {
                Body.Instructions.Insert(index, instruction);
            }
        }

        /// <summary>
        /// Inserts an instruction after another instruction.
        /// </summary>
        public void InsertAfter(MutableInstruction target, MutableInstruction instruction)
        {
            int index = Body.Instructions.IndexOf(target);
            if (index >= 0)
            {
                Body.Instructions.Insert(index + 1, instruction);
            }
        }

        /// <summary>
        /// Removes an instruction from the method body.
        /// </summary>
        public void Remove(MutableInstruction instruction)
        {
            Body.Instructions.Remove(instruction);
        }

        /// <summary>
        /// Replaces an instruction with another instruction.
        /// Updates branch targets and exception handlers that reference the old instruction.
        /// </summary>
        public void Replace(MutableInstruction oldInstruction, MutableInstruction newInstruction)
        {
            int index = Body.Instructions.IndexOf(oldInstruction);
            if (index >= 0)
            {
                Body.Instructions[index] = newInstruction;
                
                // Update branch targets
                foreach (var instr in Body.Instructions)
                {
                    if (instr.Operand == oldInstruction)
                    {
                        instr.Operand = newInstruction;
                    }
                    else if (instr.Operand is MutableInstruction[] targets)
                    {
                        for (int i = 0; i < targets.Length; i++)
                        {
                            if (targets[i] == oldInstruction)
                            {
                                targets[i] = newInstruction;
                            }
                        }
                    }
                }
                
                // Update exception handlers
                foreach (var handler in Body.ExceptionHandlers)
                {
                    if (handler.TryStart == oldInstruction) handler.TryStart = newInstruction;
                    if (handler.TryEnd == oldInstruction) handler.TryEnd = newInstruction;
                    if (handler.HandlerStart == oldInstruction) handler.HandlerStart = newInstruction;
                    if (handler.HandlerEnd == oldInstruction) handler.HandlerEnd = newInstruction;
                    if (handler.FilterStart == oldInstruction) handler.FilterStart = newInstruction;
                }
            }
        }
    }
}
