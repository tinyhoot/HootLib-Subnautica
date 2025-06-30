using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HootLib.Components
{
    /// <summary>
    /// Add this component to any semi-persistent main menu object to add your own primary option alongside the other
    /// buttons like "Play", "Options" and "Credits". The best GameObject to add this component on is the same one as
    /// <see cref="MainMenuPrimaryOptionsMenu"/>.<br />
    /// After adding this component, you should update the option's text and add one or more listeners to
    /// <see cref="onClick"/> to handle what happens when the custom option is clicked.
    /// </summary>
    /// <seealso cref="MainMenuCustomWindow"/>
    public class MainMenuCustomPrimaryOption : MonoBehaviour
    {
        public Button.ButtonClickedEvent onClick;
        [NonSerialized]
        public TextMeshProUGUI Text;
        [NonSerialized]
        public DynamicLiveTranslation DynamicTranslation;
        
        private void Awake()
        {
            // Replace the button's onClick event with our custom one.
            onClick ??= new Button.ButtonClickedEvent();
            Button button = gameObject.GetComponent<Button>();
            button.onClick = onClick;
        }

        public static MainMenuCustomPrimaryOption Create(string name, string languageKey = null)
        {
            Transform optionsContainer = uGUI_MainMenu.main.primaryOptions.transform.Find("PrimaryOptions/MenuButtons");
            
            // Create a new option by cloning the Play button.
            var gameObject = Instantiate(optionsContainer.GetChild(0).gameObject, optionsContainer, false);
            gameObject.name = name;
            var customOption = gameObject.AddComponent<MainMenuCustomPrimaryOption>();
            customOption.Text = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            
            // Add language keys for easy localisation.
            customOption.SetText(name);
            customOption.DynamicTranslation = customOption.Text.AddDynamicTranslation(languageKey);

            return customOption;
        }

        /// <summary>
        /// Move this option relative to the other options, where 0 is at the very top. There are a bunch of invisible
        /// buttons between "Play" and "Options" so you may need to set this index to a higher value than you at
        /// first expect.
        /// </summary>
        /// <param name="index"></param>
        public void SetIndex(int index)
        {
            transform.SetSiblingIndex(index);
        }

        /// <summary>
        /// Set the text displayed by this custom option. This will get overwritten if a language key has been set.
        /// </summary>
        /// <seealso cref="SetLanguageKey"/>
        public void SetText(string text)
        {
            if (!Text)
                return;
            
            Text.text = text;
        }

        /// <inheritdoc cref="DynamicLiveTranslation.SetLanguageKey(string)"/>
        public void SetLanguageKey(string key)
        {
            DynamicTranslation.SetLanguageKey(key);
        }
    }
}