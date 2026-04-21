using UnityEngine.UIElements;

namespace UISystem
{
    /// <summary>
    /// Base class for Screen-level UI managed by UIManager.
    /// </summary>
    public abstract class UIView : UIBase
    {
        // The layer this UI should reside in
        public abstract UILayer Layer { get; }
    }
}