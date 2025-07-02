using System;
using HootLib.Components;
using HootLib.Interfaces;
using TMPro;
using UnityEngine;

namespace HootLib.Configuration
{
    /// <summary>
    /// Adds pure text to the <see cref="HootModOptions"/> menu. Remember also that TextMeshPro text like this one can
    /// be styled using css tags.
    /// </summary>
    public class TextDecorator : IModOptionDecorator
    {
        protected float _fontSize;
        protected string _text;
        protected Func<object>[] _callbacks;
        protected GameObject _textObject;
        protected DynamicLiveTranslation _localiser;

        public TextDecorator(string text, float fontSize = 30f, params Func<object>[] textCallbacks)
        {
            _fontSize = fontSize;
            _text = text;
        }
        
        public virtual void AddToPanel(Transform panel)
        {
            _textObject = new GameObject("Text Label");
            _textObject.transform.SetParent(panel, false);
            var textMesh = _textObject.AddComponent<TextMeshProUGUI>();
            textMesh.font = uGUI.main.intro.mainText.text.font;
            textMesh.fontSize = _fontSize;
            textMesh.fontStyle = FontStyles.Normal;
            textMesh.enableWordWrapping = true;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.material = uGUI.main.intro.mainText.text.material;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;

            _localiser = _textObject.AddComponent<DynamicLiveTranslation>();
            _localiser.SetFormatArgCallbacks(_callbacks);
            _localiser.SetLanguageKey(_text);
        }

        public virtual void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex) { }
        
        /// <summary>
        /// Changes the text of this decorator. Also works for live updates after the mod menu has been opened.
        /// </summary>
        public virtual void SetText(string text, params Func<object>[] callbacks)
        {
            _text = text;
            if (_localiser != null)
            {
                _localiser.SetFormatArgCallbacks(callbacks);
                _localiser.SetLanguageKey(text);
            }
        }

        /// <summary>
        /// Update the displayed text by calling the callbacks provided via <see cref="SetText"/>.
        /// </summary>
        public virtual void UpdateText()
        {
            if (_localiser)
                _localiser.OnLanguageChanged();
        }
    }
}