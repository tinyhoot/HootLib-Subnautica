using UnityEngine;
using UnityEngine.UI;

namespace HootLib.Components
{
    /// <summary>
    /// A component made to be added to the GameObject of a <see cref="ScrollRect"/> which finds any ScrollBar's
    /// <see cref="Image"/> components and replaces their sprites with the vanilla menu sprites.
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
            if (scrollBarSprite is null)
                AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.ScrollBarSprite)
                    .Completed += handle => scrollBarSprite = handle.Result;
            if (scrollHandleSprite is null)
                AddressablesUtility.LoadAsync<Sprite>(AssetFilePaths.ScrollHandleSprite)
                    .Completed += handle => scrollHandleSprite = handle.Result;
        }

        private void Start()
        {
            if (scrollRect.horizontalScrollbar != null)
                SwapSprites(scrollRect.horizontalScrollbar);
            if (scrollRect.verticalScrollbar != null)
                SwapSprites(scrollRect.verticalScrollbar);
            
            // After this point, this component is no longer needed.
            Destroy(this);
        }

        private void SwapSprites(Scrollbar scrollbar)
        {
            scrollbar.image.sprite = scrollBarSprite;
            if (scrollbar.targetGraphic is Image handle)
                handle.sprite = scrollHandleSprite;
        }
    }
}