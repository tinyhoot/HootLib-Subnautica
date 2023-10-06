using System.Collections.Generic;
using HootLib.Interfaces;
using Nautilus.Options;
using UnityEngine;

namespace HootLib.Configuration
{
    /// <summary>
    /// Handles how the mod presents itself in the in-game menu.
    /// </summary>
    public class HootModOptions : ModOptions
    {
        public HootConfig Config { get; }
        protected readonly Dictionary<int, IModOptionDecorator> Decorators;

        public HootModOptions(string name, HootConfig config) : base(name)
        {
            Config = config;
            Decorators = new Dictionary<int, IModOptionDecorator>();
        }
        
        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            panel.AddHeading(modsTabIndex, Name);
            // Find the options pane for any decorators.
            Transform optionsPane = FindModOptionsPane(panel, modsTabIndex);
            // Give each decorator a chance to prepare.
            Decorators.ForEach(kv => kv.Value.PrepareDecorator(panel, modsTabIndex));
            
            var optionEnumerator = options.GetEnumerator();
            optionEnumerator.MoveNext();

            for (int idx = 0; idx < 1000; idx++)
            {
                if (Decorators.TryGetValue(idx, out IModOptionDecorator decorator))
                {
                    decorator.AddToPanel(optionsPane);
                    continue;
                }

                if (optionEnumerator.Current is null)
                    break;
                optionEnumerator.Current.AddToPanel(panel, modsTabIndex);
                optionEnumerator.MoveNext();
            }
            optionEnumerator.Dispose();

            // Ensure a newly built menu starts with the options shown or hidden correctly.
            foreach (var option in Config.GetControllingOptions())
            {
                option.UpdateControlledOptions(options, Config);
            }
        }

        /// <summary>
        /// Add a decorator at the current position to the mod menu.
        /// </summary>
        /// <param name="decorator">The decorator to add.</param>
        public virtual void AddDecorator(IModOptionDecorator decorator)
        {
            int index = Options.Count + Decorators.Count;
            Decorators.Add(index, decorator);
        }

        /// <summary>
        /// A shortcut for adding a <see cref="SeparatorDecorator"/> to the mod options menu.
        /// </summary>
        /// <param name="parent">The GameObject to parent the separator object to. This should be one which does not
        /// get trashed by the SceneCleaner on quitting the game to the main menu, or the separator will get deleted
        /// and cause this mod's section in the mod options to be empty.</param>
        public void AddSeparator(Transform parent)
        {
            AddDecorator(new SeparatorDecorator(parent));
        }

        /// <summary>
        /// A shortcut for adding a <see cref="TextDecorator"/> decorator to the mod options menu.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="fontSize">The font size of the text.</param>
        public void AddText(string text, float fontSize = 30f)
        {
            AddDecorator(new TextDecorator(text, fontSize));
        }

        protected Transform FindModOptionsPane(uGUI_TabbedControlsPanel panel, int modsTabIndex)
        {
            return panel.transform.Find("Middle/PanesHolder").GetChild(modsTabIndex).Find("Viewport/Content");
        }

        /// <summary>
        /// Check whether this panel is part of the main menu.
        /// </summary>
        public static bool IsMainMenu(uGUI_TabbedControlsPanel panel)
        {
            return panel.GetComponentInParent<uGUI_MainMenu>() != null;
        }
    }
}