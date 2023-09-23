using UnityEngine;
using UnityEngine.UI;

namespace HootLib.Components
{
    /// <summary>
    /// A simple self-destructing component meant to be slapped on any GameObject with an
    /// <see cref="UnityEngine.UI.Image"/> component to swap its sprite with the main menu's standard sprite. <br />
    /// This component really shines together with assets made in the unity editor since it is so difficult to access
    /// vanilla assets there.
    /// </summary>
    public class MainMenuSpriteSwapper : MonoBehaviour
    {
        private void Awake()
        {
            Image image = GetComponent<Image>();
            // Why am I even here? :(
            if (image is null)
                Destroy(this);

            AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.MainMenuStandardSprite).Completed += handle =>
            {
                image!.sprite = handle.Result;
                Destroy(this);
            };
        }
    }
}