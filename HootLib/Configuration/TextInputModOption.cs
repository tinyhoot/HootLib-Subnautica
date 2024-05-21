using System;
using System.Collections;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UWE;

namespace HootLib.Configuration
{
    public class TextInputModOption : ModOption<string, TextInputChangedEventArgs>
    {
        public override Type AdjusterComponent => null;
        public string PlaceholderText;
        public string Tooltip;
        private MenuTooltip _menuTooltip;
        private TextMeshProUGUI _textMesh;
        private AsyncOperationHandle<Sprite> _bgSpriteHandle;

        public TextInputModOption(string id, string label, string value, string placeholder, string tooltip) : base(label, id, value)
        {
            PlaceholderText = placeholder;
            Tooltip = tooltip;
            CoroutineHost.StartCoroutine(LoadAssets());
        }

        /// <summary>
        /// Load the asset for the background from the game.
        /// </summary>
        private IEnumerator LoadAssets()
        {
            // Generally, one should always release the handle on an asset at some point to avoid memory leaks. However,
            // this asset is part of the main menu asset bundle which remains loaded throughout, and the mod menu is
            // accessible from anywhere. Since this resource is never unloaded not releasing the handle is fine in
            // this instance.
            _bgSpriteHandle = Addressables.LoadAssetAsync<Sprite>(AssetFilePaths.EmailEntryBoxSprite);
            yield return _bgSpriteHandle;
            if (_bgSpriteHandle.Status == AsyncOperationStatus.Failed)
                Debug.LogWarning($"Failed to load asset for {nameof(TextInputModOption)}: "
                                 + $"{_bgSpriteHandle.OperationException}");
        }

        public override void AddToPanel(uGUI_TabbedControlsPanel panel, int tabIndex)
        {
            // TextInputOption
            // - TextInputContainer
            //   - Caption
            //     - CaptionText
            //   - Input
            //     - Mask
            //       - TextInput
            //         - Caret (auto-generated by TMP_InputField)
            //         - Text
            //         - Placeholder
            
            OptionGameObject = new GameObject("TextInputOption");
            LayoutElement layoutElement = OptionGameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 45;
            layoutElement.preferredHeight = 45;
            
            GameObject container = GuiUtils.CreateRectChild("TextInputContainer", OptionGameObject.transform);
            _menuTooltip = container.AddComponent<MenuTooltip>();
            _menuTooltip.key = Tooltip;
            
            GameObject caption = GuiUtils.CreateChild("Caption", container.transform);
            // Taking up the left half of the space.
            GuiUtils.SetupRectTransform(caption, Vector2.zero, new Vector2(0.47f, 1f));
            GameObject captionText = GuiUtils.CreateRectChild("CaptionText", caption.transform);
            _textMesh = GuiUtils.AddTextMeshPro(captionText, Label);
            // Without this the tooltip does not show up when hovering over the caption.
            _textMesh.raycastTarget = true;
            GameObject input = GuiUtils.CreateChild("Input", container.transform);
            // Taking up the right half of the space.
            GuiUtils.SetupRectTransform(input, new Vector2(0.5f, 0f), Vector2.one);
            CreateInputField(input.transform);
            
            // Add the object directly to the panel since AddItem() instantiates a copy.
            // This also bypasses navigation aid! The option becomes unselectable for controllers, which is not a
            // terrible thing since controllers could not properly edit it either way.
            OptionGameObject.transform.SetParent(panel.tabs[tabIndex].container, false);
            base.AddToPanel(panel, tabIndex);
        }

        /// <summary>
        /// Create the box that text can be entered into including all its subcomponents.
        /// </summary>
        private GameObject CreateInputField(Transform parent)
        {
            GameObject inputHolder = GuiUtils.CreateRectChild("Mask", parent);
            inputHolder.AddComponent<Mask>();
            var image = inputHolder.AddComponent<Image>();
            image.sprite = _bgSpriteHandle.Result;

            GameObject inputHolder2 = GuiUtils.CreateChild("TextInput", inputHolder.transform);
            SetupInputRectTransform(inputHolder2);
            
            GameObject viewPort = GuiUtils.CreateRectChild("Text", inputHolder2.transform);
            GuiUtils.AddTextMeshPro(viewPort, Value);
            GameObject placeholder = GuiUtils.CreateRectChild("Placeholder", inputHolder2.transform);
            var placeholderText = GuiUtils.AddTextMeshPro(placeholder, PlaceholderText);
            placeholderText.color = new Color(0.95f, 0.95f, 0.95f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;
            
            TMP_InputField inputField = inputHolder2.AddComponent<TMP_InputField>();
            inputField.placeholder = placeholderText;
            inputField.text = Value;
            inputField.textViewport = viewPort.GetComponent<RectTransform>();
            inputField.textComponent = viewPort.GetComponent<TextMeshProUGUI>();
            // This event fires when the player clicks away, closes the window, or otherwise stops editing.
            inputField.onEndEdit.AddListener(content => OnChange(Id, content));

            return inputHolder;
        }

        private void SetupInputRectTransform(GameObject gameObject)
        {
            GuiUtils.SetupRectTransform(gameObject, Vector2.zero, Vector2.one);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-20f, -15f);
        }
    }

    public class TextInputChangedEventArgs : ConfigOptionEventArgs<string>
    {
        public TextInputChangedEventArgs(string id, string value) : base(id, value) { }
    }
}