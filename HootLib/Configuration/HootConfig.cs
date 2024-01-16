using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HootLib.Objects.Exceptions;
using UnityEngine;

namespace HootLib.Configuration
{
    /// <summary>
    /// Holds all configurable parameters and handles the config file itself.
    /// </summary>
    public abstract class HootConfig
    {
        public ConfigFile ConfigFile { get; }
        private readonly List<ConfigEntryWrapperBase> _configEntries;
        private readonly List<ConfigEntryWrapperBase> _controllingOptions;

        protected HootConfig(ConfigFile configFile)
        {
            ConfigFile = configFile;
            _configEntries = new List<ConfigEntryWrapperBase>();
            _controllingOptions = new List<ConfigEntryWrapperBase>();
            Setup();
        }

        /// <summary>
        /// Setting up your config file happens in three steps:
        /// <list type="bullet">
        /// <item>Instantiating a new instance (this constructor).</item>
        /// <item>Calling <see cref="Setup"/> to register all options to the config file.</item>
        /// <item>Calling <see cref="RegisterModOptions"/> to register all options to the in-game menu.</item>
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
        public void Setup()
        {
            RegisterOptions();
            RegisterControllingOptions();
            UpdateControllingOptionInfo();
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
        /// Initialise, set up, and register your <see cref="HootModOptions"/> with Nautilus here. Do not forget to
        /// call this method after constructing a new config instance or you won't have an in-game menu.
        /// </summary>
        /// <param name="name">The mod name displayed as a heading.</param>
        /// <param name="persistentParent">The persistent GameObject to parent decorator object templates to. Can
        /// be null, but <see cref="HootModOptions"/> will throw an exception if you try to add a decorator that
        /// relies on it such as <see cref="SeparatorDecorator"/>.</param>
        public abstract void RegisterModOptions(string name, Transform persistentParent = null);

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
            var wrapper = new ConfigEntryWrapper<T>(ConfigFile, section, key, defaultValue, description);
            return RegisterEntry(wrapper);
        }
        
        /// <inheritdoc cref="RegisterEntry{T}(HootLib.Configuration.ConfigEntryWrapper{T})"/>
        protected ConfigEntryWrapper<T> RegisterEntry<T>(string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues)
        {
            var wrapper = new ConfigEntryWrapper<T>(ConfigFile, section, key, defaultValue, description, acceptableValues);
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
    }
}