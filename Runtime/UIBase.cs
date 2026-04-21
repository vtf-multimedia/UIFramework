using UnityEngine.UIElements;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace UISystem
{
    /// <summary>
    /// Pure C# base class for UI Toolkit Controllers with a fully asynchronous lifecycle.
    /// Handles asset loading, lifecycle hooks, and common styling.
    /// </summary>
    public abstract class UIBase
    {
        public VisualElement Root { get; protected set; }
        public bool IsVisible => Root != null && Root.style.display == DisplayStyle.Flex;

        // Path relative to Resources/
        protected abstract string UxmlPath { get; }

        /// <summary>
        /// Consolidates loading and wrapping into one async entry point.
        /// </summary>
        public async UniTask InitializeAsync(VisualElement parent)
        {
            if (parent == null) return;

            var asset = await Resources.LoadAsync<VisualTreeAsset>(UxmlPath) as VisualTreeAsset;
            if (asset == null)
            {
                Debug.LogError($"[UIBase] Failed to load VisualTreeAsset at path: Resources/{UxmlPath}");
                return;
            }

            Root = asset.Instantiate();
            if (string.IsNullOrEmpty(Root.name)) Root.name = GetType().Name;
            Root.style.flexGrow = 1;
            parent.Add(Root);

            QueryElements();
            BindEvents();
            
            StyleManager.Instance.Register(this);
            await OnInitializeAsync();
        }

        protected virtual UniTask OnInitializeAsync() => UniTask.CompletedTask;

        protected abstract void QueryElements();
        protected abstract void BindEvents();
        protected abstract void UnbindEvents();

        public virtual void RefreshStyles()
        {
            if (Root == null) return;
            StyleManager.Instance.ApplyThemeRecursive(Root);
        }

        public async UniTask ShowAsync()
        {
            if (Root == null) return;
            Root.style.display = DisplayStyle.Flex;
            await OnShowAsync();
        }

        public async UniTask HideAsync()
        {
            if (Root == null) return;
            await OnHideAsync();
            Root.style.display = DisplayStyle.None;
        }

        protected virtual async UniTask OnShowAsync()
        {
            var rule = StyleManager.Instance.GetStyle("#" + Root.name);
            if (rule?.animation?.enter != null)
            {
                // 1. Setup Initial State
                var baseState = StyleState.Default;
                if (rule.animation.initial?.style != null)
                {
                    var initialState = StyleState.Merge(baseState, rule.animation.initial.style);
                    UIStyleBridge.Apply(Root, initialState);
                }

                // 2. Animate to Enter State
                var toState = StyleState.Merge(baseState, rule.animation.enter.style);
                var fromState = rule.animation.initial?.style != null 
                    ? StyleState.Merge(baseState, rule.animation.initial.style) 
                    : baseState;

                await UIAnimation.AnimateAsync(Root, fromState, toState, rule.animation.enter.transition);
            }
        }

        protected virtual async UniTask OnHideAsync()
        {
            var rule = StyleManager.Instance.GetStyle("#" + Root.name);
            if (rule?.animation?.exit != null)
            {
                var baseState = StyleState.Default;
                var fromState = StyleState.Merge(baseState, rule.baseStyle);
                var toState = StyleState.Merge(baseState, rule.animation.exit.style);

                await UIAnimation.AnimateAsync(Root, fromState, toState, rule.animation.exit.transition);
            }
        }

        protected T Q<T>(string name = null) where T : VisualElement
        {
            if (Root == null) return null;
            return Root.Q<T>(name);
        }

        /// <summary>
        /// Final cleanup and removal from hierarchy.
        /// </summary>
        public async UniTask ReleaseAsync()
        {
            UnbindEvents();
            await OnReleaseAsync();
            
            StyleManager.Instance.Unregister(this);
            Root?.RemoveFromHierarchy();
            Root = null;
        }

        protected virtual UniTask OnReleaseAsync() => UniTask.CompletedTask;
    }
}