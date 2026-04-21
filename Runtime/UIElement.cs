using UnityEngine.UIElements;

namespace UISystem
{
    /// <summary>
    /// Base class for reusable component-level UI (Buttons, Cards, ListItems).
    /// Typically nested inside a UIView.
    /// </summary>
    public abstract class UIElement : UIBase
    {
        // Elements might have specific logic for being "selected" or "checked"
        // but they share the same base as Views.
    }
}
