using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace HootLib
{
    /// <summary>
    /// Helper methods for dealing with Transpilers and <see cref="CodeMatcher"/>s.
    /// </summary>
    public static class HootTranspiler
    {
        /// <summary>
        /// Get a <see cref="CodeMatch"/> for a variable of the given type.
        /// </summary>
        /// <param name="type">The type of the variable to search for.</param>
        /// <returns>A <see cref="CodeMatch"/> for use in <see cref="CodeMatcher"/> methods.</returns>
        /// <seealso cref="CodeMatcher.Match"/>
        public static CodeMatch VariableMatch(Type type)
        {
            return new CodeMatch(i => i.operand is LocalBuilder builder && builder.LocalType == type);
        }
        
        /// <summary>
        /// Get a <see cref="CodeMatch"/> for an instruction with the given opcode and variable of the given type.
        /// </summary>
        /// <param name="opCode">The OpCode of the instruction.</param>
        /// <param name="type">The type of the variable.</param>
        /// <returns>A <see cref="CodeMatch"/> for use in <see cref="CodeMatcher"/> methods.</returns>
        /// <seealso cref="CodeMatcher.Match"/>
        public static CodeMatch VariableMatch(OpCode opCode, Type type)
        {
            return new CodeMatch(i =>
                i.opcode == opCode && i.operand is LocalBuilder builder && builder.LocalType == type);
        }
    
        /// <summary>
        /// Get the first local variable of the given type.
        /// </summary>
        public static LocalBuilder GetFirstVariableOfType(this CodeMatcher matcher, Type type)
        {
            // Store the current position for later.
            int index = matcher.IsValid ? matcher.Pos : 0;
            matcher.Start();
            var builder = matcher.FindNextVariableOfType(type);
            // Reset the position to where we were before this function was called.
            matcher.Start().Advance(index);
            return builder;
        }
        
        /// <summary>
        /// Find the next local variable of the given type starting from the current position. Sets the matcher to that
        /// variable's position.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if no variable of the given type can be found.</exception>
        public static LocalBuilder FindNextVariableOfType(this CodeMatcher matcher, Type type)
        {
            matcher.MatchForward(false,
                new CodeMatch(i => i.operand is LocalBuilder builder && builder.LocalType == type));
            if (matcher.IsInvalid)
                throw new ArgumentException($"No local variable of type {type} found!");
            LocalBuilder builder = matcher.Operand as LocalBuilder;
            return builder;
        }

        /// <summary>
        /// Insert new instructions while preserving the original instruction's labels. Ideal for inserting at a place
        /// where other instructions jump to, e.g. at the end of an if statement.
        /// </summary>
        /// <param name="matcher">The CodeMatcher with the instructions to modify.</param>
        /// <param name="instructions">The instructions to insert.</param>
        /// <returns>The same CodeMatcher.</returns>
        public static CodeMatcher InsertAndPreserveLabels(this CodeMatcher matcher, params CodeInstruction[] instructions)
        {
            if (instructions.Length == 0)
                return matcher;
            List<CodeInstruction> toInsert = instructions.ToList();
            var original = matcher.Instruction;
            original.MoveBlocksTo(toInsert[0]);
            original.MoveLabelsTo(toInsert[0]);
            // // Add the original instruction to the very end.
            // toInsert.Add(original);
            // Insert all of our instructions before the original.
            matcher.Insert(toInsert);
            // // Return to the position of the first instruction.
            // matcher.Advance(-1);
            return matcher;
        }
    }
}