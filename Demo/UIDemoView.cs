using UnityEngine;
using UnityEngine.UIElements;
using UISystem;
using Cysharp.Threading.Tasks;

namespace UISystem.Demo
{
    /// <summary>
    /// A Demo View that hosts the UIDemoElement.
    /// Demonstrates the Parent-Child async initialization model and event handling.
    /// </summary>
    public class UIDemoView : UIView
    {
        protected override string UxmlPath => "UI/DemoView";
        public override UILayer Layer => UILayer.Screen;

        private UIDemoElement _shinyButton;

        protected override void QueryElements() { }

        protected override async UniTask OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            _shinyButton = new UIDemoElement();
            
            // The "action-btn" in DemoView.uxml acts as the anchor point/slot
            await _shinyButton.InitializeAsync(Root);
            
            _shinyButton.SetContent("LAUNCH SYSTEM");
        }

        protected override async UniTask OnShowAsync()
        {
            await base.OnShowAsync();
            if (_shinyButton != null) await _shinyButton.ShowAsync();
        }

        protected override async UniTask OnHideAsync()
        {
            if (_shinyButton != null) await _shinyButton.HideAsync();
            await base.OnHideAsync();
        }

        protected override async UniTask OnReleaseAsync()
        {
            if (_shinyButton != null) await _shinyButton.ReleaseAsync();
            await base.OnReleaseAsync();
        }

        protected override void BindEvents() 
        {
            if (_shinyButton != null)
            {
                _shinyButton.OnClick += HandleButtonClick;
            }
        }

        protected override void UnbindEvents() 
        {
            if (_shinyButton != null)
            {
                _shinyButton.OnClick -= HandleButtonClick;
            }
        }

        private void HandleButtonClick()
        {
            Debug.Log("[UIDemoView] Shiny Button Clicked! System update initiated.");
            _shinyButton?.SetContent("SYSTEM ONLINE");
        }
    }
}
