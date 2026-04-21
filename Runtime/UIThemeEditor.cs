using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// A live utility View that allows you to edit StyleManager variables in-game.
    /// demonstrating the power of the Dynamic Variable System.
    /// </summary>
    public class UIThemeEditor : UIView
    {
        protected override string UxmlPath => "UI/ThemeEditor"; // User should create this or we provide code-gen
        public override UILayer Layer => UILayer.Top;

        private VisualElement _container;
        private Button _reloadBtn;

        protected override void QueryElements()
        {
            // If UXML is missing, we can build a fallback UI in code
            _container = Q<VisualElement>("VariableList");
            _reloadBtn = Q<Button>("ReloadButton");
            
            if (_container == null) CreateFallbackUI();
        }

        private void CreateFallbackUI()
        {
            // Procedural UI for the editor if no UXML is found
            Root.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            Root.style.paddingLeft = Root.style.paddingRight = 20;
            Root.style.paddingTop = 40;

            var title = new Label("Theme Editor (Live Variables)");
            title.style.fontSize = 20;
            title.style.color = Color.white;
            title.style.marginBottom = 20;
            Root.Add(title);

            _container = new ScrollView();
            _container.name = "VariableList";
            _container.style.flexGrow = 1;
            Root.Add(_container);

            _reloadBtn = new Button(() => RefreshList());
            _reloadBtn.text = "Refresh Variable List";
            _reloadBtn.style.height = 40;
            _reloadBtn.style.marginTop = 10;
            Root.Add(_reloadBtn);
        }

        protected override void BindEvents()
        {
            RefreshList();
            StyleManager.Instance.OnThemeChanged += RefreshList;
        }

        protected override void UnbindEvents()
        {
            if (StyleManager.Instance != null)
                StyleManager.Instance.OnThemeChanged -= RefreshList;
        }

        private void RefreshList()
        {
            if (_container == null || StyleManager.Instance == null) return;
            _container.Clear();

            foreach (var kvp in StyleManager.Instance.Variables)
            {
                AddVariableField(kvp.Key, kvp.Value);
            }
        }

        public void AddVariableField(string name, string currentValue)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 5;

            var label = new Label(name);
            label.style.width = 150;
            label.style.color = Color.gray;
            row.Add(label);

            var input = new TextField();
            input.value = currentValue;
            input.style.flexGrow = 1;
            input.RegisterValueChangedCallback(evt => 
            {
                StyleManager.Instance.SetVariable(name, evt.newValue);
            });
            row.Add(input);

            _container.Add(row);
        }
    }
}
