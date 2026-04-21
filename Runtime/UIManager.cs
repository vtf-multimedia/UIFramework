using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace UISystem
{
    public enum UILayer
    {
        Background = 0,
        Screen = 10,
        Popup = 20,
        Overlay = 30,
        Top = 100
    }

    /// <summary>
    /// Manages the async lifecycle and layering of UI Toolkit Views.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private PanelSettings _panelSettings;

        private UIDocument _uiDocument;
        private VisualElement _rootLayer;
        private readonly Dictionary<UILayer, VisualElement> _layerContainers = new();
        private readonly Dictionary<Type, UIView> _activeViews = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeUIDocument();
            InitializeLayers();
        }

        private void InitializeUIDocument()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_panelSettings != null) _uiDocument.panelSettings = _panelSettings;
            
            _rootLayer = _uiDocument.rootVisualElement;
            _rootLayer.name = "UIOverlayRoot";
            _rootLayer.Clear();
        }

        private void InitializeLayers()
        {
            var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
            Array.Sort(layers, (a, b) => ((int)a).CompareTo((int)b));

            foreach (UILayer layer in layers)
            {
                VisualElement layerContainer = new VisualElement();
                layerContainer.name = $"Layer_{layer}";
                layerContainer.style.position = Position.Absolute;
                layerContainer.style.width = Length.Percent(100);
                layerContainer.style.height = Length.Percent(100);
                layerContainer.pickingMode = PickingMode.Ignore;
                
                _rootLayer.Add(layerContainer);
                _layerContainers[layer] = layerContainer;
            }
        }

        public async UniTask<T> ShowViewAsync<T>() where T : UIView, new()
        {
            Type type = typeof(T);
            
            if (_activeViews.TryGetValue(type, out var existingView))
            {
                await existingView.ShowAsync();
                return (T)existingView;
            }

            T newView = new T();
            _activeViews.Add(type, newView);
            
            VisualElement container = _layerContainers[newView.Layer];
            await newView.InitializeAsync(container);
            
            await newView.ShowAsync();
            return newView;
        }

        public async UniTask HideViewAsync<T>() where T : UIView
        {
            if (_activeViews.TryGetValue(typeof(T), out var view))
            {
                await view.HideAsync();
            }
        }

        public async UniTask DespawnViewAsync<T>() where T : UIView
        {
            Type type = typeof(T);
            if (_activeViews.TryGetValue(type, out var view))
            {
                await view.ReleaseAsync();
                _activeViews.Remove(type);
            }
        }

        public T GetView<T>() where T : UIView
        {
            if (_activeViews.TryGetValue(typeof(T), out var view))
            {
                return (T)view;
            }
            return null;
        }
    }
}