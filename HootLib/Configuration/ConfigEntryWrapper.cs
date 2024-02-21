using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx.Configuration;
using Nautilus.Options;

namespace HootLib.Configuration
{
    /// <summary>
    /// A wrapper around the BepInEx ConfigEntry which provides extra fields for more fine-grained control over
    /// ModOption behaviour in the in-game options menu.
    /// </summary>
    public class ConfigEntryWrapper<T> : ConfigEntryWrapperBase
    {
        public Dictionary<T, HashSet<string>> ControllingValues;
        public string[] OptionStringsOverride;
        public readonly ConfigEntry<T> Entry;
        public T Value => Entry.Value;

        public ConfigEntryWrapper(HootConfig config, ConfigEntry<T> entry) : base(config)
        {
            Entry = entry;
        }
        
        public ConfigEntryWrapper(HootConfig config, string section, string key, T defaultValue, string description) : base(config)
        {
            Entry = config.ConfigFile.Bind(
                section: section,
                key: key,
                defaultValue: defaultValue,
                description: description
            );
        }

        public ConfigEntryWrapper(HootConfig config, string section, string key, T defaultValue, string description,
            AcceptableValueBase acceptableValues) : base(config)
        {
            Entry = config.ConfigFile.Bind(
                section: section,
                key: key,
                defaultValue: defaultValue,
                configDescription: new ConfigDescription(description, acceptableValues)
            );
        }

        /// <summary>
        /// Prepare the entry with custom label and description for display in the mod options menu.
        /// </summary>
        public ConfigEntryWrapper<T> WithDescription(string label, string tooltip = null)
        {
            OptionLabel = label;
            OptionTooltip = tooltip;

            return this;
        }

        public override string GetSection() => Entry.Definition.Section;
        public override string GetKey() => Entry.Definition.Key;

        public override string GetId()
        {
            return Entry.Definition.Section + "_" + Entry.Definition.Key;
        }

        public override string GetLabel()
        {
            var label = OptionLabel ?? Entry.Definition.Key;
            if (NumControllingParents > 0)
                label = string.Concat(Enumerable.Repeat("   ", NumControllingParents - 1)) + $" - {label}";
            return label;
        }

        public override string GetTooltip()
        {
            return OptionTooltip ?? Entry.Description?.Description;
        }
        
        public override void UpdateControlledOptions(IEnumerable<OptionItem> options)
        {
            // Don't do anything if this option doesn't control any others.
            if (!IsControllingParent)
                return;

            ReadOnlyCollection<ConfigEntryWrapperBase> controllingOptions = Config.GetControllingOptions();
            // Collect a list of all child options which themselves also control children.
            List<ConfigEntryWrapperBase> controllerChildren = new List<ConfigEntryWrapperBase>();
            
            // Make sure the enumerator is exhausted since we have to use it twice.
            var optionItems = options as OptionItem[] ?? options.ToArray();
            foreach (var option in optionItems)
            {
                if (!ControlledOptionIds.Contains(option.Id))
                    continue;
                
                // Setting the option's GameObject inactive hides it from the menu while still retaining all the info.
                bool active = ControllingValues.GetOrDefault(Value, null)?.Contains(option.Id) ?? false;
                option.OptionGameObject.SetActive(active);
                // Add the option to the list of child controllers if it is one.
                ConfigEntryWrapperBase wrapper;
                if (active && (wrapper = controllingOptions.FirstOrDefault(entry => entry.GetId().Equals(option.Id))) != null)
                    controllerChildren.Add(wrapper);
            }
            
            // Trigger updates for each child controller.
            controllerChildren.ForEach(wrapper => wrapper.UpdateControlledOptions(optionItems));
        }

        /// <summary>
        /// For ChoiceOptions using enums only. Instead of showing the enum's values, overrides the displayed text
        /// of the choices with the contents of the given array. The array <em>must</em> contain the same number
        /// of elements as the enum!
        /// </summary>
        /// <param name="newText">The text to show for each enum value, in the same order.</param>
        public ConfigEntryWrapper<T> WithChoiceOptionStringsOverride(params string[] newText)
        {
            if (newText.Length != Enum.GetValues(typeof(T)).Length)
                throw new ArgumentException("New choices array must be the same length as the number of number of choices!");

            OptionStringsOverride = newText;
            return this;
        }

        /// <summary>
        /// Prepare the entry as a controller for displaying or hiding other options in the mod options menu.
        /// This enables showing options with preconditions, such as hiding all options of a module when that module
        /// is not active.
        /// 
        /// Options controlled in this way will only display when the value of this entry matches their precondition.
        /// It is possible to enable an option for multiple values. Useful e.g. for ChoiceOptions.
        /// </summary>
        /// <param name="enabledValue">The value this entry must be set to to enable all the other options.</param>
        /// <param name="enabledOptions">All options shown when the value is set correctly.</param>
        public ConfigEntryWrapper<T> WithConditionalOptions(T enabledValue, params ConfigEntryWrapperBase[] enabledOptions)
        {
            ControlledOptionIds ??= new List<string>();
            ControllingValues ??= new Dictionary<T, HashSet<string>>();
            if (!ControllingValues.ContainsKey(enabledValue))
                ControllingValues.Add(enabledValue, new HashSet<string>());

            foreach (var option in enabledOptions)
            {
                ControlledOptionIds.Add(option.GetId());
                ControllingValues[enabledValue].Add(option.GetId());
            }

            return this;
        }

        /// <summary><inheritdoc cref="WithConditionalOptions(T,ConfigEntryWrapperBase[])"/></summary>
        /// <param name="enabledValue"><inheritdoc cref="WithConditionalOptions(T,ConfigEntryWrapperBase[])"/></param>
        /// <param name="section">This option will have control over this entire section.</param>
        public ConfigEntryWrapper<T> WithConditionalOptions(T enabledValue, string section)
        {
            return WithConditionalOptions(enabledValue,
                Config.GetSectionEntries(section)
                    .Where(entry => !entry.GetId().Equals(this.GetId()))
                    .ToArray());
        }
    }

    public static class ConfigEntryWrapperExtensions
    {
        /// <inheritdoc cref="ToModToggleOption"/>
        public static ModChoiceOption<T> ToModChoiceOption<T>(this ConfigEntryWrapper<T> wrapper)
            where T : IEquatable<T>
        {
            T[] options = null;
            if (wrapper.Entry.Description.AcceptableValues is AcceptableValueList<T> valueList)
                options = valueList.AcceptableValues;
            if (options == null)
                throw new ArgumentException("Could not get acceptable values from ConfigEntry!");

            var modOption = ModChoiceOption<T>.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                options: options,
                value: wrapper.Value,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => 
            {
                wrapper.Entry.Value = e.Value;
                if (wrapper.Config.ModOptions != null)
                    wrapper.UpdateControlledOptions(wrapper.Config.ModOptions.Options);
            };
            if (wrapper.OptionStringsOverride != null)
                modOption.ReplaceOptionStrings(wrapper.OptionStringsOverride);
            return modOption;
        }

        /// <inheritdoc cref="ToModToggleOption"/>
        public static ModChoiceOption<TEnum> ToModChoiceOption<TEnum>(this ConfigEntryWrapper<TEnum> wrapper,
            IEnumerable<TEnum> values = null) where TEnum : Enum
        {
            TEnum[] options = values?.ToArray() ?? (TEnum[])Enum.GetValues(typeof(TEnum));
            if (options == null)
                throw new ArgumentException("Could not get acceptable values from ConfigEntry!");
            
            var modOption = ModChoiceOption<TEnum>.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                options: options,
                value: wrapper.Value,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => 
            {
                wrapper.Entry.Value = e.Value;
                if (wrapper.Config.ModOptions != null)
                    wrapper.UpdateControlledOptions(wrapper.Config.ModOptions.Options);
            };
            if (wrapper.OptionStringsOverride != null)
                modOption.ReplaceOptionStrings(wrapper.OptionStringsOverride);
            return modOption;
        }

        /// <inheritdoc cref="ToModToggleOption"/>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<int> wrapper, float minValue,
            float maxValue, float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: wrapper.Value,
                step: stepSize,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = (int)e.Value;
            return modOption;
        }

        /// <inheritdoc cref="ToModToggleOption"/>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<float> wrapper, float minValue,
            float maxValue, string valueFormat = "{0:F2}", float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: wrapper.Value,
                step: stepSize,
                valueFormat: valueFormat,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
            return modOption;
        }

        /// <inheritdoc cref="ToModToggleOption"/>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<double> wrapper, float minValue,
            float maxValue, string valueFormat = "{0:F2}", float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: (float)wrapper.Value,
                step: stepSize,
                valueFormat: valueFormat,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
            return modOption;
        }

        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModToggleOption ToModToggleOption(this ConfigEntryWrapper<bool> wrapper)
        {
            var modOption = ModToggleOption.Create(
                wrapper.GetId(),
                wrapper.GetLabel(),
                wrapper.Value,
                wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) =>
            {
                wrapper.Entry.Value = e.Value;
                if (wrapper.Config.ModOptions != null)
                    wrapper.UpdateControlledOptions(wrapper.Config.ModOptions.Options);
            };
            return modOption;
        }
    }
}