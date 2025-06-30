using System;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HootLib.Components
{
    /// <summary>
    /// A more flexible auto translator to replace <see cref="TranslationLiveUpdate"/>. Can handle both static and
    /// dynamic translations, i.e. including language lines that require variable insertion via
    /// <see cref="Language.GetFormat(string, object[])"/>.
    /// <br />
    /// Language lines will be treated as static unless <see cref="formatArgsCallbacks"/> is set.
    /// </summary>
    public class DynamicLiveTranslation : MonoBehaviour
    {
        public string translationKey;
        public Func<object>[] formatArgsCallbacks;
        public TextMeshProUGUI text;

        private Language _language;

        private void Awake()
        {
            _language = Language.main;
            
            if (!text)
                text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            Language.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Language.OnLanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// Update the text of the linked <see cref="TextMeshProUGUI"/>. Set to public so that an update can be forced
        /// manually.
        /// </summary>
        public void OnLanguageChanged()
        {
            if (!text || string.IsNullOrEmpty(translationKey))
                return;
            
            if (formatArgsCallbacks is null || formatArgsCallbacks.Length == 0)
                text.text = _language.Get(translationKey);
            else
                text.text = _language.GetFormat(translationKey, GetFormatObjects());
        }

        private object[] GetFormatObjects()
        {
            return formatArgsCallbacks.Select(cb => cb()).ToArray();
        }

        /// <summary>
        /// Set the functions that will be called to get the objects needed to fill a language line that relies on
        /// <see cref="Language.GetFormat(string, object[])"/> for dynamic content.
        /// </summary>
        public void SetFormatArgCallbacks(params Func<object>[] callbacks)
        {
            formatArgsCallbacks = callbacks;
        }

        /// <summary>
        /// Set the language key used to get the language line. Updates the text on use.
        /// </summary>
        public void SetLanguageKey(string key)
        {
            translationKey = key;
            OnLanguageChanged();
        }
    }

    public static class DynamicLiveTranslationExtensions
    {
        /// <summary>
        /// Add <see cref="DynamicLiveTranslation"/> to a text mesh. Destroys any existing
        /// <see cref="TranslationLiveUpdate"/> components.
        /// </summary>
        /// <param name="text">The <see cref="TextMeshProUGUI"/> to attach the translation component to.</param>
        /// <param name="languageKey">The key used to get the language line from <see cref="Language"/>.</param>
        /// <param name="callbacks">Optional callbacks to fill in any dynamic parts of the language line.
        /// See <see cref="Language.GetFormat(string, object[])"/>.</param>
        /// <returns>The newly added component.</returns>
        public static DynamicLiveTranslation AddDynamicTranslation(this TextMeshProUGUI text, string languageKey, params Func<object>[] callbacks)
        {
            var oldTrans = text.GetComponent<TranslationLiveUpdate>();
            if (oldTrans)
                Object.DestroyImmediate(oldTrans);

            var newTrans = text.gameObject.AddComponent<DynamicLiveTranslation>();
            newTrans.formatArgsCallbacks = callbacks;
            newTrans.SetLanguageKey(languageKey);

            return newTrans;
        }
    }
}