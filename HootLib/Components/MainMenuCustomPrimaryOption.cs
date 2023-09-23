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
        public GameObject CustomOption;
        [NonSerialized]
        public TextMeshProUGUI Text;
        
        private void Awake()
        {
            onClick ??= new Button.ButtonClickedEvent();
            
            Transform optionsContainer = uGUI_MainMenu.main.primaryOptions.transform.Find("PrimaryOptions/MenuButtons");
            // Create a new option by cloning the Play button.
            CustomOption = Instantiate(optionsContainer.GetChild(0).gameObject, optionsContainer, false);
            CustomOption.name = "HootOption";
            Text = CustomOption.GetComponentInChildren<TextMeshProUGUI>();
            SetText("Custom Option");
            // Prevent the button text from being unintentionally overwritten.
            DestroyImmediate(Text.GetComponent<TranslationLiveUpdate>());
            // Replace the button's onClick event with our custom one.
            Button button = CustomOption.GetComponent<Button>();
            button.onClick = onClick;
        }

        /// <summary>
        /// Move this option relative to the other options, where 0 is at the very top. There are a bunch of invisible
        /// buttons between "Play" and "Options" so you may need to set this index to a higher value than you at
        /// first expect.
        /// </summary>
        /// <param name="index"></param>
        public void SetIndex(int index)
        {
            CustomOption.transform.SetSiblingIndex(index);
        }

        /// <summary>
        /// Set the text displayed by this custom option. Take care not to use this before this component has had a
        /// chance to run <see cref="Awake"/> or you will get an exception.
        /// </summary>
        public void SetText(string text)
        {
            Text.text = text;
        }
    }
}