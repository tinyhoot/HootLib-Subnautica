using System.Collections.Generic;
using Nautilus.Options;

namespace HootLib.Configuration
{
    /// <summary>
    /// The base class of a config entry, without the entry and its typing attached.
    /// </summary>
    public abstract class ConfigEntryWrapperBase
    {
        public string OptionLabel;
        public string OptionTooltip;
        public List<string> ControlledOptionIds;
        public bool IsControllingParent => ControlledOptionIds?.Count > 0;
        public int NumControllingParents = 0;
        
        public abstract string GetSection();
        public abstract string GetKey();
        public abstract string GetId();
        public abstract string GetLabel();
        public abstract string GetTooltip();
        
        /// <summary>
        /// Set all other options' GameObjects to active/inactive state based on the value of this entry.
        /// </summary>
        /// <param name="options">The GameObjects of all options in the in-game menu.</param>
        /// <param name="config">The config these options originate from.</param>
        public abstract void UpdateControlledOptions(IEnumerable<OptionItem> options, HootConfig config);
    }
}