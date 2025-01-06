using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HootLib.Objects.Exceptions;
using Nautilus.Handlers;
using Newtonsoft.Json;
using UnityEngine;

namespace HootLib.Configuration
{
    /// <summary>
    /// Holds all configurable parameters and handles the config file itself.
    /// </summary>
    public abstract class HootConfig
    {
        public ConfigFile ConfigFile { get; }
        public HootModOptions ModOptions { get; private set; }
        private readonly List<ConfigEntryWrapperBase> _configEntries;
        private readonly List<ConfigEntryWrapperBase> _controllingOptions;

        protected HootConfig(ConfigFile configFile)
        {
            ConfigFile = configFile;
            _configEntries = new List<ConfigEntryWrapperBase>();
            _controllingOptions = new List<ConfigEntryWrapperBase>();
        }

        /// <summary>
        /// Setting up your config file happens in three steps:
        /// <list type="bullet">
        /// <item>Instantiating a new instance (this constructor).</item>
        /// <item>Calling <see cref="Setup"/> to register your options to the config file.</item>
        /// <item>Calling <see cref="CreateModMenu(string,UnityEngine.Transform)"/> to register all options to the in-game menu.</item>
        /// </list>
        /// </summary>
        /// <param name="path">The path to your config file.</param>
        /// <param name="metadata">The metadata of your <see cref="BepInPlugin"/> class. Available e.g. from within
        /// your BepInPlugin using <code>Info.Metadata.</code></param>
        protected HootConfig(string path, BepInPlugin metadata)
        {
            ConfigFile = new ConfigFile(path, true, metadata);
            _configEntries = new List<ConfigEntryWrapperBase>();
            _controllingOptions = new List<ConfigEntryWrapperBase>();
        }
        
        /// <summary>
        /// Calls all abstract setup options to properly register all options.
        /// Safe to use in your subclass' constructor.
        /// </summary>
        public virtual void Setup()
        {
            RegisterOptions();
            RegisterControllingOptions();
            UpdateControllingOptionInfo();
        }

        /// <summary>
        /// Register your in-game options with <see cref="HootModOptions"/> here. Do not forget to
        /// call this method after constructing a new config instance or you won't have an in-game menu.
        /// </summary>
        /// <param name="name">The mod name displayed as a heading.</param>
        /// <param name="persistentParent">The persistent GameObject to parent decorator object templates to. Can
        /// be null, but <see cref="HootModOptions"/> will throw an exception if you try to add a decorator that
        /// relies on it such as <see cref="SeparatorDecorator"/>.</param>
        public virtual void CreateModMenu(string name, Transform persistentParent = null)
        {
            CreateModMenu(new HootModOptions(name, this, persistentParent));
        }

        /// <summary>
        /// Register your in-game options with <see cref="HootModOptions"/> here. Do not forget to
        /// call this method after constructing a new config instance or you won't have an in-game menu.
        /// </summary>
        /// <param name="modOptions">A preconfigured <see cref="HootModOptions"/> or subclass thereof.</param>
        public virtual void CreateModMenu(HootModOptions modOptions)
        {
            ModOptions = modOptions;
            RegisterModOptions(ModOptions);
            OptionsPanelHandler.RegisterModOptions(ModOptions);
        }

        /// <summary>
        /// Register all options in the config file here, ideally using one of the
        /// <see cref="RegisterEntry{T}(HootLib.Configuration.ConfigEntryWrapper{T})"/> overloads.
        /// </summary>
        protected abstract void RegisterOptions();

        /// <summary>
        /// Set up all options that can toggle the display of other options in the mod menu.
        /// Delayed to its own method to ensure all options exist in the <see cref="ConfigFile"/> first.
        /// </summary>
        protected abstract void RegisterControllingOptions();

        /// <summary>
        /// Register your in-game options with <see cref="HootModOptions"/> here. Do not forget to
        /// call <see cref="CreateModMenu(string, UnityEngine.Transform)"/> after constructing a new config instance
        /// or you won't have an in-game menu.
        /// </summary>
        /// <param name="modOptions">The in-game menu to register your entries to.</param>
        /// <seealso cref="HootModOptions.AddItem"/>
        protected abstract void RegisterModOptions(HootModOptions modOptions);

        /// <summary>
        /// Register a config entry to the config. This should be used during <see cref="RegisterOptions"/>.
        /// </summary>
        protected ConfigEntryWrapper<T> RegisterEntry<T>(ConfigEntryWrapper<T> entry)
        {
            _configEntries.Add(entry);
            return entry;
        }
        
        /// <inheritdoc cref="RegisterEntry{T}(HootLib.Configuration.ConfigEntryWrapper{T})"/>
        protected ConfigEntryWrapper<T> RegisterEntry<T>(string section, string key, T defaultValue, string description)
        {
            var wrapper = new ConfigEntryWrapper<T>(this, section, key, defaultValue, description);
            return RegisterEntry(wrapper);
        }
        
        /// <inheritdoc cref="RegisterEntry{T}(HootLib.Configuration.ConfigEntryWrapper{T})"/>
        protected ConfigEntryWrapper<T> RegisterEntry<T>(string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues)
        {
            var wrapper = new ConfigEntryWrapper<T>(this, section, key, defaultValue, description, acceptableValues);
            return RegisterEntry(wrapper);
        }

        /// <summary>
        /// Update all options with the number of parents they have. Also keep track of all the parents.
        /// </summary>
        private void UpdateControllingOptionInfo()
        {
            Dictionary<string, int> parentCount = new Dictionary<string, int>();
            // First, count the exact number of parents and children.
            foreach (ConfigEntryWrapperBase entry in _configEntries)
            {
                if (!entry.IsControllingParent)
                    continue;
                
                _controllingOptions.Add(entry);
                foreach (var childId in (entry.ControlledOptionIds ?? Enumerable.Empty<string>()))
                {
                    parentCount[childId] = parentCount.GetOrDefault(childId, 0) + 1;
                }
            }
            
            // Second, relay this information to each child.
            foreach (ConfigEntryWrapperBase entry in _configEntries)
            {
                entry.NumControllingParents = parentCount.GetOrDefault(entry.GetId(), 0);
            }
        }

        /// <summary>
        /// Get all options which control the display of other options.
        /// </summary>
        public ReadOnlyCollection<ConfigEntryWrapperBase> GetControllingOptions() => _controllingOptions.AsReadOnly();

        /// <summary>
        /// Get a config entry by its id.
        /// </summary>
        /// <exception cref="ConfigEntryException">Thrown if the id does not correspond to a config entry.</exception>
        public ConfigEntryWrapperBase GetEntryById(string id)
        {
            var entry = _configEntries.Find(e => e.GetId().Equals(id));
            if (entry is null)
                throw new ConfigEntryException($"Invalid config entry id: {id}");
            
            return entry;
        }

        /// <summary>
        /// Get all config entries of the specified section.
        /// </summary>
        public IEnumerable<ConfigEntryWrapperBase> GetSectionEntries(string section)
        {
            return _configEntries.Where(wrapper => wrapper.GetSection().Equals(section));
        }
        
        public void Reload()
        {
            ConfigFile.Reload();
        }
        
        public void Save()
        {
            ConfigFile.Save();
        }

        /// <summary>
        /// Convert this config to JSON.
        /// </summary>
        /// <returns>The config entries in a single JSON string.</returns>
        public string ToJson()
        {
            List<SerializableEntry> entries = new List<SerializableEntry>();
            
            // This works because Hootils backports KeyValuePair deconstructing.
            foreach (var (definition, entry) in ConfigFile)
            {
                var se = new SerializableEntry(definition.Section, definition.Key, entry.GetSerializedValue());
                entries.Add(se);
            }

            return JsonConvert.SerializeObject(entries);
        }

        /// <summary>
        /// Attempt to populate this config with values from a JSON string.
        /// </summary>
        /// <param name="serializedEntries">The JSON string to decode. Expects a list of
        /// <see cref="SerializableEntry"/>.</param>
        public void FromJson(string serializedEntries)
        {
            List<SerializableEntry> entries = JsonConvert.DeserializeObject<List<SerializableEntry>>(serializedEntries);

            foreach (var serialEntry in entries)
            {
                ConfigDefinition definition = new ConfigDefinition(serialEntry.Section, serialEntry.Key);
                if (ConfigFile.ContainsKey(definition))
                {
                    // This is preferred over TryGet because this way gets around the generic type argument.
                    ConfigEntryBase entry = ConfigFile[definition];
                    entry.SetSerializedValue(serialEntry.Value);
                }
                else
                {
                    ErrorMessage.AddMessage($"Could not find option for entry '{serialEntry.Section}.{serialEntry.Key}'");
                }
            }
        }

        private class SerializableEntry
        {
            public string Section;
            public string Key;
            public string Value;
            
            public SerializableEntry() { }

            public SerializableEntry(string section, string key, string value)
            {
                Section = section;
                Key = key;
                Value = value;
            }
        }
    }
}