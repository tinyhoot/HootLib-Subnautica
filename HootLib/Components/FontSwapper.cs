using TMPro;
using UnityEngine;

namespace HootLib.Components
{
    /// <summary>
    /// A self-destructing component meant to be slapped on any GameObject that also contains a
    /// <see cref="TextMeshProUGUI"/> component to swap out the font for Subnautica's default font instead. <br />
    /// Mainly meant to be used with assets made in the unity editor since accessing vanilla game assets (like the font!)
    /// from there is pretty difficult.
    /// </summary>
    public class FontSwapper : MonoBehaviour
    {
        public TextMeshProUGUI textMesh;

        private void Awake()
        {
            textMesh ??= GetComponent<TextMeshProUGUI>();
            // Get the font from the intro you see when you boot up the game.
            if (textMesh != null)
                textMesh.font = uGUI.main.intro.mainText.text.font;
            Destroy(this);
        }
    }
}