using System;
using System.Collections.Generic;
using HarmonyLib;
using HootLib.Interfaces;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
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
        protected readonly Transform PersistentParent;
        
        /// <summary>
        /// Invoked for every mod option that is constructed and added to the menu during <see cref="BuildModOptions"/>.
        /// <br/>
        /// Use this to edit option GameObjects directly when they "go live", such as to disable interaction while
        /// still displaying them.
        /// <seealso cref="DisableOption"/>
        /// </summary>
        public event Action<AddOptionToMenuEventArgs> OnAddOptionToMenu;

        /// <summary>
        /// Create an in-game menu based on a config file. Note that if you do not provide a persistent parent transform,
        /// using methods that add special decorators like <see cref="AddSeparator"/> will throw an exception.
        /// </summary>
        /// <param name="name">The name of your mod's section in the mod menu.</param>
        /// <param name="config">The config forming the basis of this menu.</param>
        public HootModOptions(string name, HootConfig config) : base(name)
        {
            Config = config;
            Decorators = new Dictionary<int, IModOptionDecorator>();
        }
        
        /// <inheritdoc cref="HootModOptions(string, HootConfig)"/>
        /// <param name="persistentParent">The GameObject to parent the separator object to. This should be one which
        /// does not get trashed by the SceneCleaner on quitting the game to the main menu, or the separator will get
        /// deleted, cause a NullRef and result in this mod's section in the mod options to be empty.</param>
        public HootModOptions(string name, HootConfig config, Transform persistentParent) : base(name)
        {
            Config = config;
            Decorators = new Dictionary<int, IModOptionDecorator>();
            PersistentParent = persistentParent;
        }
        
        /// <summary>
        /// Builds the in-game mod menu. This method may be called whenever the player clicks the button to open the
        /// game's options menu, so expect it to execute multiple times.
        /// </summary>
        /// <param name="panel">The panel of the options window that will hold the options.</param>
        /// <param name="modsTabIndex">The index of the tab that is responsible for mod options.</param>
        /// <param name="options">A collection of all options that had previously been registered.</param>
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
                option.UpdateControlledOptions(options);
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
        public void AddSeparator()
        {
            if (PersistentParent == null)
                throw new NullReferenceException($"{nameof(PersistentParent)} was not assigned, cannot create a "
                                                 + $"separator!");
            AddDecorator(new SeparatorDecorator(PersistentParent));
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

        /// <summary>
        /// Ensure all options displayed in the mod options menu are in sync with the values in the config file.
        /// </summary>
        public void Validate()
        {
            foreach (var option in Options)
            {
                Toggle toggle = option.OptionGameObject.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    ValidateToggleOption(toggle, option.Id);
                    continue;
                }
                
                uGUI_Choice choice = option.OptionGameObject.GetComponentInChildren<uGUI_Choice>();
                if (choice != null)
                {
                    ValidateChoiceOption(choice, option.Id);
                    continue;
                }

                uGUI_SnappingSlider slider = option.OptionGameObject.GetComponentInChildren<uGUI_SnappingSlider>();
                if (slider != null)
                {
                    ValidateSliderOption(slider, option.Id);
                    continue;
                }

                TMP_InputField inputField = option.OptionGameObject.GetComponentInChildren<TMP_InputField>();
                if (inputField != null)
                {
                    ValidateInputFieldOption(inputField, option.Id);
                    continue;
                }
                
                Debug.LogWarning($"Failed to find corresponding mod option type for option id '{option.Id}'");
            }
        }

        /// <summary>
        /// The base class of the config entry doesn't have access to the typed value, so do some magic to extract it.
        /// </summary>
        private object ExtractConfigValue(ConfigEntryWrapperBase wrapperBase)
        {
            // ConfigEntryWrapper<T> entry = wrapperBase as ConfigEntryWrapper<T>;
            // For some insane reason casting does not work, despite the types seemingly matching perfectly.
            // Instead, skip the wrapper and just get the value directly using reflection.
            var propInfo = AccessTools.Property(wrapperBase.GetType(), nameof(ConfigEntryWrapper<string>.Value));
            return propInfo.GetValue(wrapperBase);
        }

        /// <summary>
        /// Ensure a <see cref="uGUI_Choice"/> option reflects the value as written in the config file.
        /// </summary>
        private void ValidateChoiceOption(uGUI_Choice choice, string id)
        {
            var entry = Config.GetEntryById(id);
            Type type = entry.GetType().GetGenericArguments()[0];
            object value = ExtractConfigValue(entry);
            
            // Try to set the value for enum-based choices.
            if (type.IsEnum || typeof(int).IsAssignableFrom(type))
            {
                choice.value = (int)value;
            }
            else
            {
                // Handle string-based choices.
                int idx = choice.options.IndexOf(value.ToString());
                choice.value = idx;
            }
        }

        /// <summary>
        /// Ensure a <see cref="TMP_InputField"/> option reflects the value as written in the config file.
        /// </summary>
        private void ValidateInputFieldOption(TMP_InputField inputField, string id)
        {
            var entry = Config.GetEntryById(id);
            object value = ExtractConfigValue(entry);
            inputField.text = value.ToString();
        }

        /// <summary>
        /// Ensure a <see cref="uGUI_SnappingSlider"/> option reflects the value as written in the config file.
        /// </summary>
        private void ValidateSliderOption(uGUI_SnappingSlider slider, string id)
        {
            var entry = Config.GetEntryById(id);
            object value = ExtractConfigValue(entry);
            // You cannot cast directly from object to float so sadly this is necessary.
            switch (value)
            {
                case int i:
                    slider.Set(i);
                    return;
                case float f:
                    slider.Set(f);
                    return;
                case double d:
                    slider.Set((float)d);
                    return;
                default:
                    ErrorMessage.AddMessage($"Failed to import value '{value}' for option '{id}'.");
                    break;
            }
        }

        /// <summary>
        /// Ensure a <see cref="Toggle"/> option reflects the value as written in the config file.
        /// </summary>
        private void ValidateToggleOption(Toggle toggle, string id)
        {
            var entry = (ConfigEntryWrapper<bool>)Config.GetEntryById(id);
            toggle.isOn = entry.Value;
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