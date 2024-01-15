using System;
using HootLib.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HootLib.Configuration
{
    /// <summary>
    /// Acts as a visual separator for the <see cref="HootModOptions"/> menu.
    /// </summary>
    public class SeparatorDecorator : IModOptionDecorator
    {
        protected static GameObject _separator;
        protected Transform _parent;

        /// <summary>
        /// Creates a GameObject which can act as a visual separator in the options menu. All separators added via
        /// <see cref="AddToPanel"/> create a cloned copy.
        /// </summary>
        /// <param name="parent">The GameObject to parent the separator object to. This should be one which does not
        /// get trashed by the SceneCleaner on quitting the game to the main menu, or the separator will get deleted
        /// and cause this mod's section in the mod options to be empty.</param>
        public SeparatorDecorator(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Cannot create separator when no separator parent was provided!");
            _parent = parent;
        }
        
        public void AddToPanel(Transform panel)
        {
            Object.Instantiate(_separator, panel, false);
        }

        public void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex)
        {
            _separator ??= CreateSeparator(panel, _parent);
        }
        
        /// <summary>
        /// Creates a GameObject which can act as a visual separator in the options menu. All separators added via
        /// <see cref="AddToPanel"/> create a cloned copy of this one.
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
            image.sprite = Hootils.RecolorSprite(sprite, new Color(0.4f, 0.7f, 0.9f));
            return separator;
        }
    }
}