using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Component = UnityEngine.Component;

namespace UIFramework
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase : MonoBehaviour
    {
        // --- Dependencies ---
        public Canvas Canvas { get; private set; }
        public GraphicRaycaster Raycaster { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }

        // --- Child Tracking ---
        [SerializeField] private List<UIComponent> _uiComponents = new();

        [ContextMenu("Fetch Immediate UI Components")]
        public void FetchUIComponents()
        {
            _uiComponents.Clear();
            foreach (Transform child in transform)
            {
                var component = child.GetComponent<UIComponent>();
                if (component != null)
                {
                    _uiComponents.Add(component);
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Fetch All UI Components (Deep)")]
        public void FetchUIComponentsDeep()
        {
            _uiComponents.Clear();
            _uiComponents.AddRange(GetComponentsInChildren<UIComponent>(true));
            _uiComponents.Remove(this as UIComponent); // Remove self if this is a UIComponent
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Show")]
        public void ShowUI() => Show(false).Forget();

        [ContextMenu("Hide")]
        public void HideUI() => Hide(false).Forget();


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
            Canvas = GetComponent<Canvas>();
            Raycaster = GetComponent<GraphicRaycaster>();
            CanvasGroup = GetComponent<CanvasGroup>();
            
            // 2. Polymorphic Setup (Views vs Components)
            ConfigureCanvas();

            if (UnityEngine.Application.isPlaying)
            {
                IsVisible = false;
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }

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

            // 1. Force Layout Rebuild (Critical for correct positioning/sizing)
            if (Canvas != null) Canvas.ForceUpdateCanvases();

            // 2. User Logic Hook
            await OnShow();
            
            // 3. Chain Children
            foreach (var uiComponent in _uiComponents)
            {
                await uiComponent.Show(instant);
            }
        }

        public async UniTask Hide(bool instant = false)
        {
            if (!IsVisible && !gameObject.activeSelf) return;

            IsVisible = false;

            foreach (var uiComponent in _uiComponents)
            {
                await uiComponent.Hide(instant);
            }

            // 1. User Logic Hook
            await OnHide();

            // 2. Deactivate
            gameObject.SetActive(false);
        }

        // ===================================================================================
        // 2. INTERACTION & UTILITY
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