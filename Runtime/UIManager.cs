using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UIFramework
{
    // Ensure the Manager itself is on a Canvas (or creates one)
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // --- Layers (Created at Runtime) ---
        private RectTransform _bgRoot;
        private RectTransform _screenRoot;
        private RectTransform _modalRoot;
        private RectTransform _widgetRoot;
        
        // --- System ---
        private CanvasGroup _modalCurtain;

        // --- State Tracking ---
        private UIView _currentBackground;
        private UIView _currentScreen;
        private Stack<UIView> _modalStack = new Stack<UIView>();
        private List<UIView> _activeWidgets = new List<UIView>();
        
        private Dictionary<string, UIView> _viewCache = new Dictionary<string, UIView>();

        // --- Z-Order Constants ---
        private const int ORDER_BG = 0;
        private const int ORDER_SCREEN = 100;
        private const int ORDER_MODAL = 1000;
        private const int ORDER_WIDGET = 2000;

        private Canvas _canvas;

        private void Awake()
        {
            Instance = this;
            Initialize();
            InitializeRoots();
            InitializeCurtain();
        }

        // ===================================================================================
        // INITIALIZATION LOGIC (Auto-Generate Hierarchy)
        // ===================================================================================

        private void Initialize()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private void InitializeRoots()
        {
            // 1. Background Layer (Bottom)
            _bgRoot = CreateLayerRoot("Layer_0_Background");
            
            // 2. Screen Layer
            _screenRoot = CreateLayerRoot("Layer_1_Screen");
            
            // 3. Modal Layer
            _modalRoot = CreateLayerRoot("Layer_2_Modal");
            
            // 4. Widget Layer (Top)
            _widgetRoot = CreateLayerRoot("Layer_3_Widget");
        }

        private RectTransform CreateLayerRoot(string layerName)
        {
            // Create GO
            GameObject obj = new GameObject(layerName);
            obj.transform.SetParent(this.transform, false);

            // Add RectTransform
            RectTransform rect = obj.AddComponent<RectTransform>();
            
            // Stretch Logic: Min(0,0) Max(1,1)
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; // Left/Bottom
            rect.offsetMax = Vector2.zero; // Right/Top
            rect.localScale = Vector3.one;

            return rect;
        }

        private void InitializeCurtain()
        {
            // Create the dark background blocker for modals
            GameObject curtainObj = new GameObject("System_ModalCurtain");
            curtainObj.transform.SetParent(_modalRoot, false); // Sit inside Modal Root

            // Full Stretch
            RectTransform rect = curtainObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Visuals
            Image img = curtainObj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f); // Semi-transparent black
            img.raycastTarget = true; // Block clicks

            // Components
            _modalCurtain = curtainObj.AddComponent<CanvasGroup>();
            _modalCurtain.alpha = 0f;
            curtainObj.SetActive(false);
            
            // Add Canvas for Sorting
            Canvas c = curtainObj.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            
            curtainObj.AddComponent<GraphicRaycaster>();
        }

        // ===================================================================================
        // 1. PUBLIC API - GETTERS
        // ===================================================================================

        public UIView GetView(string viewID)
        {
            if (_viewCache.TryGetValue(viewID, out var view)) return view;

            var prefab = Resources.Load<UIView>($"UI/Views/{viewID}");
            if (prefab == null) 
            {
                Debug.LogError($"[UI] View '{viewID}' not found in Resources.");
                return null;
            }

            var instance = Instantiate(prefab);
            instance.name = viewID;
            instance.gameObject.SetActive(false); 
            _viewCache[viewID] = instance;
            
            return instance;
        }

        public T GetView<T>(string viewID) where T : UIView
        {
            var view = GetView(viewID);
            return view as T;
        }

        // ===================================================================================
        // 2. PUBLIC API - SHOW
        // ===================================================================================

        public async UniTask<UIView> Show(UIView view)
        {
            if (view == null) return null;
            await InternalRouteView(view);
            return view;
        }

        public async UniTask<UIView> Show(string viewID)
        {
            UIView view = GetView(viewID);
            if (view == null) return null;
            await InternalRouteView(view);
            return view;
        }

        public async UniTask<T> Show<T>(T view) where T : UIView
        {
            if (view == null) return null;
            await InternalRouteView(view);
            return view;
        }

        // ===================================================================================
        // 3. PUBLIC API - HIDE & BACK
        // ===================================================================================

        public async UniTask Back()
        {
            if (_modalStack.Count > 0) await CloseTopModal();
        }

        public async UniTask Hide(UIView view)
        {
            if (view == null) return;

            if (view.Type == ViewType.Modal && _modalStack.Count > 0 && _modalStack.Peek() == view)
            {
                await CloseTopModal();
            }
            else
            {
                await view.Hide();
            }
        }

        public async UniTask Hide(string viewID)
        {
            if (_viewCache.TryGetValue(viewID, out var view)) await Hide(view);
        }

        // ===================================================================================
        // 4. INTERNAL ROUTING LOGIC
        // ===================================================================================

        private async UniTask InternalRouteView(UIView view)
        {
            switch (view.Type)
            {
                case ViewType.Background: await ShowBackground(view); break;
                case ViewType.Screen:     await ShowScreen(view);     break;
                case ViewType.Modal:      await ShowModal(view);      break;
                case ViewType.Widget:     await ShowWidget(view);     break;
            }
        }

        private async UniTask ShowBackground(UIView view)
        {
            view.transform.SetParent(_bgRoot, false);
            view.SetSortingOrder(ORDER_BG);

            var oldBg = _currentBackground;
            _currentBackground = view;

            var showTask = view.Show();
            await showTask;

            if (oldBg != null && oldBg != view) await oldBg.Hide();
        }

        private async UniTask ShowScreen(UIView view)
        {
            await CloseAllModals();

            view.transform.SetParent(_screenRoot, false);
            view.SetSortingOrder(ORDER_SCREEN);

            if (_currentScreen != null && _currentScreen != view) 
                await _currentScreen.Hide();

            _currentScreen = view;
            await view.Show();
            
        }

        private async UniTask ShowModal(UIView view)
        {
            view.transform.SetParent(_modalRoot, false);
            
            // Dynamic Sort: Base + (Depth * 10)
            int order = ORDER_MODAL + (_modalStack.Count * 10);
            view.SetSortingOrder(order);

            // Curtain goes immediately behind this modal
            UpdateCurtain(true, order - 1);

            if (!_modalStack.Contains(view)) _modalStack.Push(view);
            
            await view.Show();
        }

        private async UniTask ShowWidget(UIView view)
        {
            view.transform.SetParent(_widgetRoot, false);
            
            int order = ORDER_WIDGET + _activeWidgets.Count;
            view.SetSortingOrder(order);
            
            if (!_activeWidgets.Contains(view)) _activeWidgets.Add(view);

            _ = view.Show();
            await UniTask.CompletedTask;
        }

        // ===================================================================================
        // 5. HELPER LOGIC
        // ===================================================================================

        private async UniTask CloseAllModals()
        {
            Stack<UIView> keepStack = new Stack<UIView>();

            while (_modalStack.Count > 0)
            {
                UIView modal = _modalStack.Pop(); 
                modal.Hide(); 
            }

            while (keepStack.Count > 0) _modalStack.Push(keepStack.Pop());
            
            if (_modalStack.Count == 0) UpdateCurtain(false, 0);
            else UpdateCurtain(true, _modalStack.Peek().Canvas.sortingOrder - 1);
        }

        private async UniTask CloseTopModal()
        {
            if (_modalStack.Count == 0) return;

            UIView top = _modalStack.Pop();
            await top.Hide();

            if (_modalStack.Count > 0)
            {
                UIView next = _modalStack.Peek();
                UpdateCurtain(true, next.Canvas.sortingOrder - 1);
            }
            else
            {
                UpdateCurtain(false, 0);
            }
        }

        private void UpdateCurtain(bool active, int order)
        {
            if (_modalCurtain == null) return;
            
            _modalCurtain.gameObject.SetActive(active);
            _modalCurtain.alpha = active ? 1f : 0f;
            _modalCurtain.blocksRaycasts = active;
            
            if (_modalCurtain.TryGetComponent<Canvas>(out var c))
            {
                c.overrideSorting = true;
                c.sortingOrder = order;
            }
        }
    }
}