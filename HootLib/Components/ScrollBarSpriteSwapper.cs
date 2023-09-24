using UnityEngine;
using UnityEngine.UI;

namespace HootLib.Components
{
    /// <summary>
    /// A self-destructing component made to be added to the GameObject of a <see cref="ScrollRect"/> which finds any
    /// ScrollBar's <see cref="Image"/> components and replaces their sprites with the vanilla menu sprites.
    /// Ideal for making a scrollbar look just like the vanilla ones found in the main menu.
    /// </summary>
    public class ScrollBarSpriteSwapper : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public Sprite scrollBarSprite;
        public Sprite scrollHandleSprite;
        
        private void Awake()
        {
            scrollRect ??= GetComponent<ScrollRect>();
            scrollBarSprite ??= AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.ScrollBarSprite)
                .WaitForCompletion();
            scrollHandleSprite ??= AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.ScrollHandleSprite)
                .WaitForCompletion();
            
            if (scrollRect.horizontalScrollbar != null)
                SwapSprites(scrollRect.horizontalScrollbar);
            if (scrollRect.verticalScrollbar != null)
                SwapSprites(scrollRect.verticalScrollbar);
            
            // After this point, this component is no longer needed.
            Destroy(this);
        }

        private void SwapSprites(Scrollbar scrollbar)
        {
            scrollbar.GetComponent<Image>().sprite = scrollBarSprite;
            scrollbar.image.sprite = scrollHandleSprite;
        }
    }
}