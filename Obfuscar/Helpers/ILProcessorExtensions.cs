using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Obfuscar.Helpers
{
    static class ILProcessorExtensions
    {
        public static void ReplaceAndFixReferences(this ILProcessor processor, MethodBody body, Dictionary<Instruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (KeyValuePair<Instruction, LdStrInstructionReplacement> oldToNew in oldToNewStringInstructions)
            {
                Instruction oldInstruction = oldToNew.Key;
                int oldIndex = oldToNew.Value.OldIndex;
                Instruction newInstruction = oldToNew.Value.NewInstruction;

                newInstruction.Offset = oldInstruction.Offset;
                body.Instructions[oldIndex] = newInstruction;
            }

            ReplaceInstructions(processor, oldToNewStringInstructions);

            ReplaceExceptionHandlers(processor, oldToNewStringInstructions);
        }

        private static void ReplaceInstructions(ILProcessor processor, Dictionary<Instruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (Instruction bodyInstruction in processor.Body.Instructions)
            {
                ReplaceOperandInstruction(bodyInstruction, oldToNewStringInstructions);
            }
        }

        private static void ReplaceOperandInstruction(Instruction bodyInstruction, Dictionary<Instruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            if (bodyInstruction == null)
            {
                return;
            }

            if (bodyInstruction.Operand is Instruction instructionOperand)
            {
                if (oldToNewStringInstructions.TryGetValue(instructionOperand, out LdStrInstructionReplacement oldToNew))
                {
                    bodyInstruction.Operand = oldToNew.NewInstruction;
                }
            }
            else if (bodyInstruction.Operand is Instruction[] instructionArrayOperand)
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

        private static void ReplaceExceptionHandlers(ILProcessor processor, Dictionary<Instruction, LdStrInstructionReplacement> oldToNewStringInstructions)
        {
            foreach (ExceptionHandler exceptionHandler in processor.Body.ExceptionHandlers)
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
            ( Instruction instruction
            , Action<Instruction> setInstruction
            , Dictionary<Instruction, LdStrInstructionReplacement> oldToNewStringInstructions
            )
        {
            if (instruction != null && oldToNewStringInstructions.TryGetValue(instruction, out LdStrInstructionReplacement oldToNew))
            {
                setInstruction(oldToNew.NewInstruction);
            }
        }
    }
}
