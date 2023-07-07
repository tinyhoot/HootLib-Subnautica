using System;
using HarmonyLib;
using Nautilus.Options;

namespace HootLib.Configuration
{
    public static class ModOptionExtensions
    {
        // This is very likely to break if Nautilus changes too much about the ModChoiceOption class.
        private const string _choiceOptionStrings = "OptionStrings";
        
        /// <summary>
        /// Replaces the option strings of ModChoiceOptions. This effectively allows for arbitrary text display without
        /// disturbing the selection process under the hood.<br />
        /// This reflection fuckery is not how you should do things, but Nautilus is a bit cagey about access modifiers.
        /// </summary>
        public static ModChoiceOption<T> ReplaceOptionStrings<T>(this ModChoiceOption<T> option, string[] newChoices)
        {
            if (option.Options.Length != newChoices.Length)
                throw new ArgumentException("New choices array must be the same length as the number of choices!");
            var field = AccessTools.GetDeclaredFields(option.GetType())
                .Find(f => f.Name.Contains(_choiceOptionStrings));
            field.SetValue(option, newChoices);

            return option;
        }
    }
}