namespace UIFramework
{
    public enum ViewType
    {
        Background, // Layer 0: Loading Screens, 3D Scenes
        Screen,     // Layer 100: Main Menu, Gameplay HUD (Exclusive context)
        Modal,      // Layer 1000+: Inventory, Settings, Confirmations (Stacked)
        Widget      // Layer 2000+: Floating Text, Tooltips, Toasts (Parallel)
    }
}