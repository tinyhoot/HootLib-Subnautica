using UnityEngine;

namespace HootLib.Components
{
    /// <summary>
    /// A component made to interact with <see cref="uGUI_MainMenu"/> to "open" and "close" the main menu while
    /// setting the GameObject this component is attached to active/inactive, which allows for custom primary
    /// options in the main menu.
    ///
    /// The methods of this component are best used in tandem with the event functions of a
    /// <see cref="UnityEngine.UI.Button"/>.
    /// </summary>
    /// <seealso cref="MainMenuCustomPrimaryOption"/>
    public class MainMenuCustomWindow : MonoBehaviour
    {
        /// <summary>
        /// Close the window and return to the main menu.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            uGUI_MainMenu.main.ClosePanel();
        }

        /// <summary>
        /// Open the window and hide the main menu.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            uGUI_MainMenu.main.ShowPrimaryOptions(false);
        }
    }
}