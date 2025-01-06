using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HootLib
{
    /// <summary>
    /// A collection of methods to facilitate working with UI elements through code.
    /// </summary>
    public static class GuiUtils
    {
        /// <summary>
        /// Create and set up a <see cref="TextMeshProUGUI"/> component which looks just like vanilla UI text.
        /// </summary>
        public static TextMeshProUGUI AddTextMeshPro(GameObject gameObject, string text, float fontSize = 32f)
        {
            TextMeshProUGUI textMesh = gameObject.EnsureComponent<TextMeshProUGUI>();
            textMesh.font = uGUI.main.intro.mainText.text.font;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Normal;
            textMesh.enableWordWrapping = true;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.material = uGUI.main.intro.mainText.text.material;
            textMesh.text = text;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;

            return textMesh;
        }
        
        /// <summary>
        /// Create a new GameObject with the given name under the given parent.
        /// </summary>
        public static GameObject CreateChild(string name, Transform parent, bool keepWorldPosition = false)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, keepWorldPosition);
            return child;
        }

        /// <summary>
        /// Create a new GameObject with a RectTransform under the given parent that fills its space.
        /// </summary>
        public static GameObject CreateRectChild(string name, Transform parent, bool keepWorldPosition = false)
        {
            GameObject child = CreateChild(name, parent, keepWorldPosition);
            SetupRectTransform(child);
            return child;
        }

        /// <summary>
        /// Create the basis for a mod option with a box that text can be entered into.
        /// </summary>
        /// <param name="backgroundSprite">The sprite to display for the background of the text field.</param>
        /// <param name="value">The default starting value of the field. May be null or empty.</param>
        /// <param name="placeholder">The placeholder text to display while the input field is empty.</param>
        /// <param name="tooltip">The text to show when hovering over the option.</param>
        /// <param name="contentType">Constrains the content the user can enter.</param>
        /// <param name="caption">If this is included, adds a caption to the left half of the option.</param>
        public static GameObject CreateTextInputOption(Sprite backgroundSprite, string value, 
            string placeholder, string tooltip, TMP_InputField.ContentType contentType, string caption = null)
        {
            // TextInputOption
            // - TextInputContainer
            //   - Caption (optional)
            //     - CaptionText
            //   - Input (handoff)
            
            // Set the holding object to the usual options menu height.
            GameObject OptionGameObject = new GameObject("TextInputOption");
            LayoutElement layoutElement = OptionGameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 45;
            layoutElement.preferredHeight = 45;
            
            // Mostly exists in case a caption has to be added. Putting the tooltip on this makes it work for both
            // caption and input field.
            GameObject container = CreateRectChild("TextInputContainer", OptionGameObject.transform);
            MenuTooltip menuTooltip = container.AddComponent<MenuTooltip>();
            menuTooltip.key = tooltip;
            
            if (caption != null)
            {
                // The caption takes up the left half of the available space.
                GameObject captionObject = CreateChild("Caption", container.transform);
                SetupRectTransform(captionObject, Vector2.zero, new Vector2(0.47f, 1f));
                GameObject captionText = CreateRectChild("CaptionText", captionObject.transform);
                TextMeshProUGUI textMesh = AddTextMeshPro(captionText, caption);
                // Without this the tooltip does not show up when hovering over the caption.
                textMesh.raycastTarget = true;
            }
            
            // The input field takes up all remaining available space.
            GameObject input = CreateChild("Input", container.transform);
            // If a caption exists, only take the right half.
            Vector2 anchorMin = new Vector2(caption is null ? 0f : 0.5f, 0f);
            SetupRectTransform(input, anchorMin, Vector2.one);
            CreateInputField(input.transform, backgroundSprite, value, placeholder, contentType);

            return OptionGameObject;
        }
        
        /// <summary>
        /// Create a box that text can be entered into including all its subcomponents.
        /// </summary>
        /// <param name="parent">The holding object to parent the InputField to. Must have a <see cref="RectTransform"/>
        /// component.</param>
        /// <param name="backgroundSprite">The sprite to display for the background of the text field.</param>
        /// <param name="value">The default starting value of the field. May be null or empty.</param>
        /// <param name="placeholder">The placeholder text to display while the input field is empty.</param>
        /// <param name="contentType">Constrains the content the user can enter.</param>
        public static GameObject CreateInputField(Transform parent, Sprite backgroundSprite, string value, 
            string placeholder, TMP_InputField.ContentType contentType)
        {
            // - Input (parent)
            //   - Mask
            //     - TextInput
            //       - Caret (auto-generated by TMP_InputField)
            //       - Text
            //       - Placeholder
            
            // The mask prevents the text from escaping beyond the bounds of the parent object.
            GameObject inputHolder = CreateRectChild("Mask", parent);
            inputHolder.AddComponent<Mask>();
            var image = inputHolder.AddComponent<Image>();
            image.sprite = backgroundSprite;

            GameObject inputHolder2 = CreateChild("TextInput", inputHolder.transform);
            SetupInputRectTransform(inputHolder2);
            
            // These objects do not get auto-generated by TMP_InputField. Set up the basics.
            GameObject viewPort = CreateRectChild("Text", inputHolder2.transform);
            AddTextMeshPro(viewPort, value);
            GameObject placeholderObject = CreateRectChild("Placeholder", inputHolder2.transform);
            var placeholderText = AddTextMeshPro(placeholderObject, placeholder);
            placeholderText.color = new Color(0.95f, 0.95f, 0.95f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;
            
            TMP_InputField inputField = inputHolder2.AddComponent<TMP_InputField>();
            inputField.placeholder = placeholderText;
            inputField.text = value;
            inputField.textViewport = viewPort.GetComponent<RectTransform>();
            inputField.textComponent = viewPort.GetComponent<TextMeshProUGUI>();
            inputField.contentType = contentType;

            return inputHolder;
        }
        
        /// <summary>
        /// Set up a <see cref="RectTransform"/> but size it properly for use with a <see cref="TMP_InputField"/>.
        /// </summary>
        public static void SetupInputRectTransform(GameObject gameObject)
        {
            SetupRectTransform(gameObject, Vector2.zero, Vector2.one);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-20f, -15f);
        }
        
        /// <summary>
        /// Ensure a <see cref="RectTransform"/> on an object and set it up to fill the space of its parent.
        /// </summary>
        public static void SetupRectTransform(GameObject gameObject)
        {
            // Use the maximum available space.
            SetupRectTransform(gameObject, Vector2.zero, Vector2.one);
        }

        /// <inheritdoc cref="SetupRectTransform(UnityEngine.GameObject)"/>
        public static void SetupRectTransform(GameObject gameObject, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = gameObject.EnsureComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
        }
    }
}