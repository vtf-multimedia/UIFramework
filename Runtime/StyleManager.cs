using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace UIFramework
{
    public class StyleManager : MonoBehaviour
    {
        public static StyleManager Instance { get; private set; }
        public event Action OnThemeChanged;

        private Dictionary<string, UIElementStyle> _registry;
        private string _fullPath;
        private FileSystemWatcher _watcher;
        private bool _reloadPending = false;

        private void Awake()
        {
            Instance = this;
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
            try {
                _registry = StyleParser.Parse(File.ReadAllText(_fullPath));
                OnThemeChanged?.Invoke();
            } catch (Exception e) { Debug.LogError($"JSON Parse Error: {e.Message}"); }
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

        public UIElementStyle Resolve(string id, List<string> classes)
        {
            if (_registry == null) return null;
            if (!string.IsNullOrEmpty(id) && _registry.TryGetValue("#" + id, out var idStyle)) return idStyle;
            if (classes != null) {
                foreach (var c in classes) if (_registry.TryGetValue("." + c, out var clsStyle)) return clsStyle;
            }
            return null;
        }
    }
}