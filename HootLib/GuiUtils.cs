using TMPro;
using UnityEngine;

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