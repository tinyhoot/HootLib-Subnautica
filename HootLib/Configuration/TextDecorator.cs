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
        private float _fontSize;
        private string _text;

        public TextDecorator(string text, float fontSize = 30f)
        {
            _fontSize = fontSize;
            _text = text;
        }
        
        public void AddToPanel(Transform panel)
        {
            var textObject = new GameObject("Text Label");
            textObject.transform.SetParent(panel, false);
            var textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.font = uGUI.main.intro.mainText.text.font;
            textMesh.fontSize = _fontSize;
            textMesh.fontStyle = FontStyles.Normal;
            textMesh.enableWordWrapping = true;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.material = uGUI.main.intro.mainText.text.material;
            textMesh.text = _text;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        public void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex) { }
    }
}