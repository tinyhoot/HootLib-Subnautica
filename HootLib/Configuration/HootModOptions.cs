using System;
using System.Collections.Generic;
using HootLib.Interfaces;
using Nautilus.Options;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HootLib.Configuration
{
    /// <summary>
    /// Handles how the mod presents itself in the in-game menu.
    /// </summary>
    public class HootModOptions : ModOptions
    {
        public HootConfig Config { get; }
        protected readonly Dictionary<int, IModOptionDecorator> Decorators;
        
        /// <summary>
        /// Invoked for every mod option that is constructed and added to the menu during <see cref="BuildModOptions"/>.
        /// <br/>
        /// Use this to edit option GameObjects directly when they "go live", such as to disable interaction while
        /// still displaying them.
        /// <seealso cref="DisableOption"/>
        /// </summary>
        public event Action<AddOptionToMenuEventArgs> OnAddOptionToMenu;

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
            
            // Cache this so we don't look it up for every single item later.
            bool isMainMenu = IsMainMenu(panel);
            var optionEnumerator = options.GetEnumerator();
            // Init the enumerator.
            optionEnumerator.MoveNext();

            for (int idx = 0; idx < 1000; idx++)
            {
                // Try to insert any decorators first. Only if that fails, move on to option items.
                if (Decorators.TryGetValue(idx, out IModOptionDecorator decorator))
                {
                    decorator.AddToPanel(optionsPane);
                    continue;
                }

                if (optionEnumerator.Current is null)
                    break;
                
                // Add the option to the menu.
                OptionItem item = optionEnumerator.Current;
                item.AddToPanel(panel, modsTabIndex);
                // Let listeners modify the option's GameObject for specialised behaviour.
                OnAddOptionToMenu?.Invoke(new AddOptionToMenuEventArgs(item.Id, item.OptionGameObject, isMainMenu));
                
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

        /// <summary>
        /// A convenience method for marking a mod option's <em>already existing</em> GameObject as uninteractable
        /// with visual distinction. This method will add and edit <see cref="CanvasGroup"/> and <see cref="Image"/>
        /// components to the object if it does not already have them.
        /// <seealso cref="OnAddOptionToMenu"/>
        /// </summary>
        /// <param name="option">The GameObject of the mod option.</param>
        /// <param name="disabledColor">The color overlaid on the option. For a slight tinge, a low alpha is
        /// recommended.</param>
        /// <param name="visibleToRaycasts">If false, completely stops the option from receiving any mouse events. Among
        /// other things, this causes Tooltips not to appear on hover.</param>
        public static void DisableOption(GameObject option, Color disabledColor, bool visibleToRaycasts = true)
        {
            var canvas = option.EnsureComponent<CanvasGroup>();
            // Make raycasts ignore the option if necessary.
            canvas.blocksRaycasts = visibleToRaycasts;
            // Even if the option is able to receive raycasts it may not be interacted with.
            canvas.interactable = false;

            // Change the option visually to indicate the difference to the user.
            var image = option.EnsureComponent<Image>();
            // Grab RoundedCornerBackground from an object that we know always exists when we need it.
            image.sprite = Object.FindObjectOfType<uGUI_HardcoreGameOver>().transform.Find("Message").GetComponent<Image>().sprite;
            image.type = Image.Type.Sliced;
            image.color = disabledColor;
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

    /// <summary>
    /// Contains an option being added to a mod menu during the menu's build process. Provides the id of the option,
    /// the GameObject that represents it, and whether the mod menu it is being added to is part of the main menu.
    /// </summary>
    public struct AddOptionToMenuEventArgs
    {
        public string ID;
        public GameObject GameObject;
        public bool IsMainMenu;
        
        public AddOptionToMenuEventArgs(string id, GameObject gameObject, bool isMainMenu)
        {
            ID = id;
            GameObject = gameObject;
            IsMainMenu = isMainMenu;
        }
    }
}