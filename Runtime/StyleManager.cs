using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System;

namespace UISystem
{
    [DefaultExecutionOrder(-100)]
    public class StyleManager : MonoBehaviour
    {
        public static StyleManager Instance { get; private set; }
        public event Action OnThemeChanged;

        private Dictionary<string, UIElementStyle> _registry;
        private Dictionary<string, string> _variables = new();
        public IReadOnlyDictionary<string, string> Variables => _variables;
        private readonly List<UIBase> _activeControllers = new();
        private readonly HashSet<VisualElement> _interactionBoundElements = new();
        
        private string _fullPath;
        private FileSystemWatcher _watcher;
        private bool _reloadPending = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _fullPath = Path.Combine(Application.streamingAssetsPath, "style.json");
            LoadThemeFromDisk();
            SetupWatcher();
        }

        private void Update()
        {
            if (_reloadPending) { _reloadPending = false; LoadThemeFromDisk(); }
        }

        private void OnDestroy() { if (_watcher != null) _watcher.Dispose(); }

        private void LoadThemeFromDisk()
        {
            if (!File.Exists(_fullPath)) return;
            try
            {
                var theme = StyleParser.Parse(File.ReadAllText(_fullPath));
                _registry = theme.styles;
                _variables = theme.variables;
                
                Debug.Log($"[StyleManager] Theme loaded: {_registry.Count} styles, {_variables.Count} variables.");
                RefreshAll();
                OnThemeChanged?.Invoke();
            }
            catch (Exception e) { Debug.LogError($"[StyleManager] JSON Parse Error: {e.Message}"); }
        }

        private void SetupWatcher()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            string dir = Path.GetDirectoryName(_fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _watcher = new FileSystemWatcher(dir, Path.GetFileName(_fullPath));
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += (s, e) => _reloadPending = true;
            _watcher.EnableRaisingEvents = true;
#endif
        }

        public void Register(UIBase controller)
        {
            if (!_activeControllers.Contains(controller))
            {
                _activeControllers.Add(controller);
                controller.RefreshStyles();
            }
        }

        public void Unregister(UIBase controller) => _activeControllers.Remove(controller);

        public void RefreshAll()
        {
            _interactionBoundElements.Clear();
            foreach (var controller in _activeControllers)
            {
                controller.RefreshStyles();
            }
        }

        public void SetVariable(string name, string value)
        {
            string key = name.TrimStart('-');
            _variables[key] = value;
            RefreshAll();
        }

        public string GetVariable(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string cleanName = name.Replace("var(", "").Replace(")", "").TrimStart('-');
            if (_variables.TryGetValue(cleanName, out var val)) return val;
            return null;
        }

        public UIElementStyle GetStyle(string selector)
        {
            if (_registry == null) return null;
            if (_registry.TryGetValue(selector, out var style)) return style;
            return null;
        }

        public void ApplyThemeRecursive(VisualElement root)
        {
            if (_registry == null || root == null) return;

            // 1. Apply Classes
            foreach (var rule in _registry)
            {
                if (rule.Key.StartsWith("."))
                {
                    string className = rule.Key.Substring(1);
                    root.Query(className: className).ForEach(el => {
                        UIStyleBridge.Apply(el, rule.Value.baseStyle);
                        BindInteractions(el, rule.Value);
                    });
                    
                    if (root.ClassListContains(className)) {
                        UIStyleBridge.Apply(root, rule.Value.baseStyle);
                        BindInteractions(root, rule.Value);
                    }
                }
            }

            // 2. Apply IDs
            foreach (var rule in _registry)
            {
                if (rule.Key.StartsWith("#"))
                {
                    string idName = rule.Key.Substring(1);
                    var element = root.Q(idName);
                    if (element != null) {
                        UIStyleBridge.Apply(element, rule.Value.baseStyle);
                        BindInteractions(element, rule.Value);
                    }
                    
                    if (root.name == idName) {
                        UIStyleBridge.Apply(root, rule.Value.baseStyle);
                        BindInteractions(root, rule.Value);
                    }
                }
            }
        }

        private void BindInteractions(VisualElement element, UIElementStyle rule)
        {
            if (rule.animation == null) return;
            
            // Avoid double-binding during a single refresh pass
            if (_interactionBoundElements.Contains(element)) return;

            // Ensure the element can receive pointer events
            if (rule.animation.hover != null || rule.animation.press != null)
            {
                element.pickingMode = PickingMode.Position;
            }
            
            // 1. HOVER
            if (rule.animation.hover?.style != null)
            {
                element.RegisterCallback<PointerEnterEvent>(evt => {
                    UIStyleBridge.Apply(element, rule.animation.hover.style);
                });
                element.RegisterCallback<PointerLeaveEvent>(evt => {
                    UIStyleBridge.Apply(element, rule.baseStyle);
                });
            }

            // 2. PRESS
            if (rule.animation.press?.style != null)
            {
                element.RegisterCallback<PointerDownEvent>(evt => {
                    UIStyleBridge.Apply(element, rule.animation.press.style);
                });
                element.RegisterCallback<PointerUpEvent>(evt => {
                    // Revert to hover if mouse is still over, otherwise base
                    bool isOver = element.panel != null && element.panel.Pick(evt.position) == element;
                    var target = isOver ? rule.animation.hover?.style : null;
                    UIStyleBridge.Apply(element, target ?? rule.baseStyle);
                });
            }

            _interactionBoundElements.Add(element);
        }
    }

    public static class StyleVariableExtensions
    {
        public static string Resolve(this string value)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains("var(")) return value;
            if (StyleManager.Instance == null) return value;
            string resolved = StyleManager.Instance.GetVariable(value);
            return resolved ?? value;
        }
    }
}