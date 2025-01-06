using System;
using System.Collections;
using HootLib.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UWE;

namespace HootLib.Configuration
{
    /// <summary>
    /// A freely editable text box. The difference to <see cref="TextInputModOption"/> is that this decorator is not
    /// tied to a config setting and can instead be used for more transient purposes.
    /// </summary>
    public class TextInputDecorator : IModOptionDecorator
    {
        public string PlaceholderText;
        public string Tooltip;
        private GameObject _optionGameObject;
        private TMP_InputField _inputField;
        private TMP_InputField.ContentType _contentType;
        private AsyncOperationHandle<Sprite> _bgSpriteHandle;

        public TextInputDecorator(string placeholder, string tooltip, TMP_InputField.ContentType contentType)
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
        
        public void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex) { }

        public void AddToPanel(Transform panel)
        {
            _optionGameObject = GuiUtils.CreateTextInputOption(_bgSpriteHandle.Result, "", PlaceholderText, Tooltip,
                _contentType);
            _inputField = _optionGameObject.GetComponentInChildren<TMP_InputField>();
            _optionGameObject.transform.SetParent(panel, false);
        }

        /// <summary>
        /// Gets the current contents of the text input field.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the method is called while the input field does not
        /// exist, such as anytime outside the options menu.</exception>
        public string GetContent()
        {
            if (_inputField == null)
                throw new InvalidOperationException("Cannot get contents of non-existing input field.");
            
            return _inputField.text;
        }

        /// <summary>
        /// Sets the current contents of the text input field.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the method is called while the input field does not
        /// exist, such as anytime outside the options menu.</exception>
        public void SetContent(string content)
        {
            if (_inputField == null)
                throw new InvalidOperationException("Cannot get contents of non-existing input field.");

            _inputField.text = content;
        }
    }
}