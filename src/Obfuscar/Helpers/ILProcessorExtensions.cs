using System;
using System.Collections.Generic;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Helpers
{
    /// <summary>
    /// Extension methods for MutableILProcessor to support instruction replacement.
    /// </summary>
    /// <remarks>
    /// These extensions are used during string obfuscation to replace ldstr instructions
    /// with calls to string accessor methods, while preserving branch targets and
    /// exception handler boundaries.
    /// </remarks>
    static class ILProcessorExtensions
    {
        /// <summary>
        /// Replaces instructions and fixes all references (branch targets, exception handlers).
        /// </summary>
        public static void ReplaceAndFixReferences(this MutableILProcessor processor, MutableMethodBody body, Dictionary<MutableInstruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (KeyValuePair<MutableInstruction, LdStrInstructionReplacement> oldToNew in oldToNewStringInstructions)
            {
                MutableInstruction oldInstruction = oldToNew.Key;
                int oldIndex = oldToNew.Value.OldIndex;
                MutableInstruction newInstruction = oldToNew.Value.NewInstruction;

                newInstruction.Offset = oldInstruction.Offset;
                body.Instructions[oldIndex] = newInstruction;
            }

            ReplaceInstructions(processor, oldToNewStringInstructions);

            ReplaceExceptionHandlers(processor, oldToNewStringInstructions);
        }

        private static void ReplaceInstructions(MutableILProcessor processor, Dictionary<MutableInstruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (MutableInstruction bodyInstruction in processor.Body.Instructions)
            {
                ReplaceOperandInstruction(bodyInstruction, oldToNewStringInstructions);
            }
        }

        private static void ReplaceOperandInstruction(MutableInstruction bodyInstruction, Dictionary<MutableInstruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            if (bodyInstruction == null)
            {
                return;
            }

            if (bodyInstruction.Operand is MutableInstruction instructionOperand)
            {
                if (oldToNewStringInstructions.TryGetValue(instructionOperand, out LdStrInstructionReplacement oldToNew))
                {
                    bodyInstruction.Operand = oldToNew.NewInstruction;
                }
            }
            else if (bodyInstruction.Operand is MutableInstruction[] instructionArrayOperand)
            {
                for (int i = 0; i < instructionArrayOperand.Length; i++)
                {
                    if (oldToNewStringInstructions.TryGetValue(instructionArrayOperand[i], out LdStrInstructionReplacement oldToNew))
                    {
                        instructionArrayOperand[i] = oldToNew.NewInstruction;
                    }
                }

                bodyInstruction.Operand = instructionArrayOperand;
            }
        }

        private static void ReplaceExceptionHandlers(MutableILProcessor processor, Dictionary<MutableInstruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (MutableExceptionHandler exceptionHandler in processor.Body.ExceptionHandlers)
            {
                ReplaceInstruction(
                    exceptionHandler.FilterStart,
                    instruction => exceptionHandler.FilterStart = instruction,
                    oldToNewStringInstructions);
                ReplaceInstruction(
                    exceptionHandler.HandlerStart,
                    instruction => exceptionHandler.HandlerStart = instruction,
                    oldToNewStringInstructions);
                ReplaceInstruction(
                    exceptionHandler.HandlerEnd,
                    instruction => exceptionHandler.HandlerEnd = instruction,
                    oldToNewStringInstructions);
                ReplaceInstruction(
                    exceptionHandler.TryStart,
                    instruction => exceptionHandler.TryStart = instruction,
                    oldToNewStringInstructions);
                ReplaceInstruction(
                    exceptionHandler.TryEnd,
                    instruction => exceptionHandler.TryEnd = instruction,
                    oldToNewStringInstructions);
            }
        }

        private static void ReplaceInstruction
            ( MutableInstruction instruction
            , Action<MutableInstruction> setInstruction
            , Dictionary<MutableInstruction, LdStrInstructionReplacement> oldToNewStringInstructions
            )
        {
            if (instruction != null && oldToNewStringInstructions.TryGetValue(instruction, out LdStrInstructionReplacement oldToNew))
            {
                setInstruction(oldToNew.NewInstruction);
            }
        }
    }
}
