using HootLib.Configuration;
using UnityEngine;

namespace HootLib.Interfaces
{
    /// <summary>
    /// A decorating or informative element that can be freely added to a mod options menu, but is not itself an option.
    /// </summary>
    public interface IModOptionDecorator
    {
        /// <summary>
        /// Add this Decorator to the mod options panel.
        /// </summary>
        /// <param name="panel">The Transform containing all mod options content.</param>
        public void AddToPanel(Transform panel);
        
        /// <summary>
        /// Called only once as <see cref="HootModOptions.BuildModOptions"/> is first called, i.e. on every rebuild of
        /// the options menu.
        /// </summary>
        /// <param name="panel">The options panel.</param>
        /// <param name="modsTabIndex">The index of the tab containing the mod options.</param>
        public void PrepareDecorator(uGUI_TabbedControlsPanel panel, int modsTabIndex);
    }
}