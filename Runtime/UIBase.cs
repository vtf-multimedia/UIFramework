using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Component = UnityEngine.Component;

namespace UIFramework
{
    // THE BUNDLE: Enforce the full toolset on every element
    [RequireComponent(typeof(UIIdentity))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(UIStyle))]
    [RequireComponent(typeof(UIAnimation))]
    public abstract class UIBase : MonoBehaviour
    {
        // --- Dependencies ---
        public UIIdentity Identity { get; private set; }
        public Canvas Canvas { get; private set; }
        public GraphicRaycaster Raycaster { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }
        public UIStyle StyleComponent { get; private set; }
        public UIAnimation Animation { get; private set; }

        // --- Child Tracking ---
        protected List<UIBase> ChildUIComponents { get; private set; } = new List<UIBase>();

        // --- State Flags ---
        public bool IsVisible { get; protected set; }
        public bool IsHovered { get; protected set; }
        public bool IsPressed { get; protected set; }
        public bool IsSelected { get; protected set; }
        public bool IsChecked { get; protected set; }

        public event Action OnStateChanged;

        protected virtual void Awake()
        {
            // 1. Fetch Dependencies
            Identity = GetComponent<UIIdentity>();
            Canvas = GetComponent<Canvas>();
            Raycaster = GetComponent<GraphicRaycaster>();
            CanvasGroup = GetComponent<CanvasGroup>();
            StyleComponent = GetComponent<UIStyle>();
            Animation = GetComponent<UIAnimation>();

            // Find all immediate UIBase children
            FetchChildUIBases();

            // 2. Polymorphic Setup (Views vs Components)
            ConfigureCanvas();

            // 3. Default state (Hidden)
            // Fix CS0104: Explicitly use UnityEngine.Application
            if (UnityEngine.Application.isPlaying)
            {
                IsVisible = false;
                gameObject.SetActive(false);
            }
        }

        private void FetchChildUIBases()
        {
            ChildUIComponents.Clear();
            // Get all nested UIBase components
            var allChildren = GetComponentsInChildren<UIBase>(true);
            foreach (var child in allChildren)
            {
                if (child == this) continue;

                // Only track immediate UIBase children to avoid double-triggering grandchildren
                var parentUI = child.transform.parent != null
                    ? child.transform.parent.GetComponentInParent<UIBase>(true)
                    : null;

                if (parentUI == this)
                {
                    ChildUIComponents.Add(child);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (StyleManager.Instance != null)
                StyleManager.Instance.OnThemeChanged += OnHotReload;
        }

        protected virtual void OnDisable()
        {
            if (StyleManager.Instance != null)
                StyleManager.Instance.OnThemeChanged -= OnHotReload;
        }

        // --- ABSTRACT MEMBERS ---

        /// <summary>
        /// Defines how this element sits in the sorting hierarchy.
        /// (e.g. Views override sorting, Components inherit it)
        /// </summary>
        protected abstract void ConfigureCanvas();

        /// <summary>
        /// Logic hook called BEFORE the show animation starts.
        /// Use this for data binding, resetting scroll views, etc.
        /// </summary>
        protected abstract UniTask OnShow();

        /// <summary>
        /// Logic hook called BEFORE the hide animation starts.
        /// Use this for cleanup, saving state, etc.
        /// </summary>
        protected abstract UniTask OnHide();

        // ===================================================================================
        // 1. PUBLIC ASYNC API (Lifecycle)
        // ===================================================================================

        public async UniTask Show(bool instant = false)
        {
            if (IsVisible && gameObject.activeSelf) return;

            IsVisible = true;
            gameObject.SetActive(true);

            // 1. Prepare
            Animation.Stop();

            // 2. Apply Style (Snap visuals to "Start" state)
            RefreshStyle();

            // 3. Force Layout Rebuild (Critical for correct positioning/sizing)
            if (Canvas != null) Canvas.ForceUpdateCanvases();

            // 4. User Logic Hook
            await OnShow();

            // 5. Play Animation
            // Create tasks for all children to show simultaneously
            IEnumerable<UniTask> childTasks = ChildUIComponents.Select(c => c.Show(instant));

            if (instant)
            {
                Animation.PlayState("normal");
                await UniTask.WhenAll(childTasks);
            }
            else
            {
                // Play container animation and children simultaneously
                await UniTask.WhenAll(Animation.PlayShow(), UniTask.WhenAll(childTasks));
            }
        }

        public async UniTask Hide(bool instant = false)
        {
            if (!IsVisible && !gameObject.activeSelf) return;

            IsVisible = false;

            // Cascade hide to children first so they animate out before parent disappears
            IEnumerable<UniTask> childTasks = ChildUIComponents.Select(c => c.Hide(instant));
            await UniTask.WhenAll(childTasks);

            // 1. User Logic Hook
            await OnHide();

            // 2. Play Animation
            if (!instant)
            {
                await Animation.PlayHide();
            }

            // 3. Deactivate
            gameObject.SetActive(false);
        }

        // ===================================================================================
        // 2. STYLING & HOT RELOAD
        // ===================================================================================

        private void OnHotReload()
        {
            // React to JSON changes instantly without full Show sequence
            RefreshStyle();
        }

        protected void RefreshStyle()
        {
            if (StyleManager.Instance == null) return;

            // Resolve Definition
            var elementStyle = StyleManager.Instance.Resolve(Identity.ID, Identity.Classes);

            if (elementStyle != null)
            {
                // A. Apply Static Base Style (Inspector + JSON Base)
                if (elementStyle.BaseStyle != null)
                {
                    StyleComponent.ApplyDefinition(elementStyle.BaseStyle);
                }

                // B. Pass Animation Config to Animator (Hover/Press definitions)
                if (elementStyle.Animation != null)
                {
                    Animation.Setup(elementStyle.Animation);

                    // Force refresh current visual state (e.g. if we were hovering, update hover color)
                    NotifyStateChange();
                }
            }
        }

        // ===================================================================================
        // 3. INTERACTION & UTILITY
        // ===================================================================================

        protected void SetState(string stateName, bool value)
        {
            switch (stateName)
            {
                case "hover": IsHovered = value; break;
                case "press": IsPressed = value; break;
                case "select": IsSelected = value; break;
                case "check": IsChecked = value; break;
            }

            NotifyStateChange();
        }

        // Fix CS1061: Ensure this is public so UIManager can call it on UIView
        public void SetSortingOrder(int order)
        {
            if (Canvas != null)
            {
                Canvas.overrideSorting = true; // Required for nested canvases to sort independently
                Canvas.sortingOrder = order;
            }
        }

        protected void NotifyStateChange()
        {
            OnStateChanged?.Invoke();

            // Resolve priority (Check > Pressed > Hover > Normal)
            string key = ResolveStateKey();

            // Trigger Animation Tween
            Animation.PlayState(key);
        }

        public virtual string ResolveStateKey()
        {
            if (IsChecked) return "check";
            if (IsPressed) return "press";
            if (IsHovered) return "hover";
            if (IsSelected) return "select";
            return "normal";
        }
    }
}