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
        protected GameObject _textObject;

        public TextDecorator(string text, float fontSize = 30f)
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
            textMesh.text = _text;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        public virtual void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex) { }
        
        /// <summary>
        /// Changes the text of this decorator. Also works for live updates after the mod menu has been opened.
        /// </summary>
        public void SetText(string text)
        {
            _text = text;
            if (_textObject != null)
            {
                var textMesh = _textObject.GetComponent<TextMeshProUGUI>();
                if (textMesh != null)
                    textMesh.SetText(text);
            }
        }
    }
}