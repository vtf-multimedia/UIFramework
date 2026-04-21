using UnityEngine.UIElements;
using UISystem;
using Cysharp.Threading.Tasks;

namespace UISystem.Demo
{
    /// <summary>
    /// A reusable Demo Button component that now manages its own UXML.
    /// </summary>
    public class UIDemoElement : UIElement
    {
        protected override string UxmlPath => "UI/DemoElement"; 

        public event System.Action OnClick;

        private Label _label;
        private VisualElement _icon;

        protected override void QueryElements()
        {
            _label = Q<Label>("button-text");
            _icon = Q<VisualElement>("button-icon");
        }

        public void SetContent(string text)
        {
            if (_label != null) _label.text = text;
        }

        protected override void BindEvents() 
        {
            Root.RegisterCallback<ClickEvent>(OnRootClicked);
        }

        protected override void UnbindEvents() 
        {
            Root.UnregisterCallback<ClickEvent>(OnRootClicked);
        }

        private void OnRootClicked(ClickEvent evt)
        {
            OnClick?.Invoke();
        }
    }
}
