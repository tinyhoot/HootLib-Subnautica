using System;
using System.Collections;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UWE;

namespace HootLib.Configuration
{
    /// <summary>
    /// Used to add an option to <see cref="HootModOptions"/> that comes with a freely editable text box.
    /// </summary>
    public class TextInputModOption : ModOption<string, TextInputChangedEventArgs>
    {
        public override Type AdjusterComponent => null;
        public string PlaceholderText;
        public string Tooltip;
        private TMP_InputField.ContentType _contentType;
        private AsyncOperationHandle<Sprite> _bgSpriteHandle;

        public TextInputModOption(string id, string label, string value, string placeholder, string tooltip, 
            TMP_InputField.ContentType contentType) : base(label, id, value)
        {
            PlaceholderText = placeholder;
            Tooltip = tooltip;
            _contentType = contentType;
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
            OptionGameObject = GuiUtils.CreateTextInputOption(_bgSpriteHandle.Result, Value, PlaceholderText, Tooltip,
                _contentType, Label);
            TMP_InputField inputField = OptionGameObject.GetComponentInChildren<TMP_InputField>();
            // Ensure the text is saved to the config.
            // This event fires when the player clicks away, closes the window, or otherwise stops editing.
            inputField.onEndEdit.AddListener(content => OnChange(Id, content));
            
            // Add the object directly to the panel since AddItem() instantiates a copy.
            // This also bypasses navigation aid! The option becomes unselectable for controllers, which is not a
            // terrible thing since controllers could not properly edit it either way.
            OptionGameObject.transform.SetParent(panel.tabs[tabIndex].container, false);
            base.AddToPanel(panel, tabIndex);
        }
    }

    public class TextInputChangedEventArgs : ConfigOptionEventArgs<string>
    {
        public TextInputChangedEventArgs(string id, string value) : base(id, value) { }
    }
}