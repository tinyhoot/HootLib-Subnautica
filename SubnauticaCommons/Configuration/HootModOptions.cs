using System.Collections.Generic;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaCommons.Configuration
{
    /// <summary>
    /// Handles how the mod presents itself in the in-game menu.
    /// </summary>
    public class HootModOptions : ModOptions
    {
        public HootConfig Config { get; }
        private Transform _modOptionsPane;
        private GameObject _separator;
        protected readonly Transform SeparatorParent;
        protected readonly List<string> AddSeparatorBefore;

        public HootModOptions(string name, HootConfig config, Transform separatorParent) : base(name)
        {
            Config = config;
            SeparatorParent = separatorParent;
            AddSeparatorBefore = new List<string>();
        }
        
        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            // Reset the options pane reference to avoid linking to a gameobject that was destroyed.
            FindModOptionsPane(panel, modsTabIndex);
            panel.AddHeading(modsTabIndex, Name);

            foreach (var option in options)
            {
                if (AddSeparatorBefore.Contains(option.Id))
                    AddSeparator(panel);
                option.AddToPanel(panel, modsTabIndex);
            }

            // Ensure a newly built menu starts with the options shown or hidden correctly.
            foreach (var option in Config.GetControllingOptions())
            {
                option.UpdateControlledOptions(options, Config);
            }
        }

        /// <summary>
        /// Add a visual separator to the given menu.
        /// </summary>
        protected virtual void AddSeparator(uGUI_TabbedControlsPanel panel)
        {
            _separator ??= CreateSeparator(panel, SeparatorParent);
            Object.Instantiate(_separator, _modOptionsPane, false);
        }

        /// <summary>
        /// Add a visual separator before each option with an id matching one of the ids provided to this method.
        /// </summary>
        public void AddSeparatorBeforeOption(params string[] optionIds)
        {
            AddSeparatorBefore.AddRange(optionIds);
        }
        
        /// <summary>
        /// Add a pure text label without attachment to any particular option to the menu.
        /// </summary>
        protected virtual void AddText(string text, float fontSize = 30f)
        {
            var textObject = new GameObject("Text Label");
            textObject.transform.SetParent(_modOptionsPane, false);
            var textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.autoSizeTextContainer = true;
            textMesh.font = uGUI.main.intro.mainText.text.font;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Normal;
            textMesh.enableWordWrapping = true;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.material = uGUI.main.intro.mainText.text.material;
            textMesh.text = text;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
        }
        
        /// <summary>
        /// Creates a GameObject which can act as a visual separator in the options menu. All separators added via
        /// <see cref="AddSeparator"/> create a cloned copy of this one.
        /// </summary>
        /// <param name="panel">The main menu panel containing the mod options.</param>
        /// <param name="parent">The GameObject to parent the separator object to. This should be one which does not
        /// get trashed by the SceneCleaner on quitting the game to the main menu, or the separator will get deleted
        /// and cause this mod's section in the mod options to be empty.</param>
        protected GameObject CreateSeparator(uGUI_TabbedControlsPanel panel, Transform parent)
        {
            GameObject separator = new GameObject("OptionSeparator");
            separator.layer = 5;
            separator.transform.SetParent(parent);
            Transform panesHolder = panel.transform.Find("Middle/PanesHolder");

            LayoutElement layout = separator.EnsureComponent<LayoutElement>();
            layout.minHeight = 40;
            layout.preferredHeight = 40;
            layout.minWidth = -1;

            // Putting the image into its own child object prevents weird layout issues.
            GameObject background = new GameObject("Background");
            background.transform.SetParent(separator.transform, false);
            // Get the image from one of the sliders in the "General" tab.
            Image image = background.EnsureComponent<Image>();
            // SliderOption sprite - very nice, but very yellow
            Sprite sprite = panesHolder.GetChild(0).Find("Viewport/Content/uGUI_SliderOption(Clone)/Slider/Slider/Background").GetComponent<Image>().sprite;
            //image.sprite = panesHolder.GetChild(0).Find("Scrollbar Vertical/Sliding Area/Handle").GetComponent<Image>().sprite;
            // The size of the image gameobject will auto-adjust to the image. This fixes it.
            float targetWidth = panel.transform.GetComponent<RectTransform>().rect.width * 0.67f;
            background.transform.localScale = new Vector3(targetWidth / background.GetComponent<RectTransform>().rect.width, 0.4f, 1f);
            
            // Change the colour of the nabbed sprite.
            image.sprite = Utils.RecolorSprite(sprite, new Color(0.4f, 0.7f, 0.9f));

            _separator = separator;
            return separator;
        }

        protected void FindModOptionsPane(uGUI_TabbedControlsPanel panel, int modsTabIndex)
        {
            _modOptionsPane = panel.transform.Find("Middle/PanesHolder").GetChild(modsTabIndex).Find("Viewport/Content");
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