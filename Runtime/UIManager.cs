using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UIFramework
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // --- Multi-Display Architecture ---
        public class DisplayRoot
        {
            public int DisplayIndex;
            public Canvas RootCanvas;
            
            public RectTransform BgRoot;
            public RectTransform ScreenRoot;
            public RectTransform ModalRoot;
            public RectTransform WidgetRoot;
            
            public CanvasGroup ModalCurtain;
            public Stack<UIView> ModalStack = new Stack<UIView>();
        }

        // Registry
        private Dictionary<int, DisplayRoot> _displays = new Dictionary<int, DisplayRoot>();

        // Global Cache
        private Dictionary<string, UIView> _viewCache = new Dictionary<string, UIView>();
        private List<UIView> _activeWidgets = new List<UIView>();

        // Z-Order Constants
        private const int ORDER_BG = 0;
        private const int ORDER_SCREEN = 100;
        private const int ORDER_MODAL = 1000;
        private const int ORDER_WIDGET = 2000;

        private void Awake()
        {
            Instance = this;
            
            // Generate the Main Display (Index 0) immediately as a child
            CreateDisplayRoot(0);
        }

        // ===================================================================================
        // 1. PUBLIC API - GETTERS
        // ===================================================================================

        public UIView GetView(string viewID, int targetDisplayIndex = 0)
        {
            // 1. Check Cache
            if (_viewCache.TryGetValue(viewID, out var view)) 
            {
                // Ensure it lives on the requested display if explicitly asked
                // (Optional: You might want to skip this check if you want to support moving views later)
                int currentDisplay = GetDisplayIndexOfView(view);
                if (currentDisplay != targetDisplayIndex)
                {
                    SetViewToDisplay(view, targetDisplayIndex);
                }
                return view;
            }

            // 2. Load
            var prefab = Resources.Load<UIView>($"UI/Views/{viewID}");
            if (prefab == null) 
            {
                Debug.LogError($"[UI] View '{viewID}' not found in Resources.");
                return null;
            }

            // 3. Instantiate
            var instance = Instantiate(prefab);
            instance.name = viewID;
            instance.gameObject.SetActive(false); 
            _viewCache[viewID] = instance;

            // 4. Initialize on Target Display
            SetViewToDisplay(instance, targetDisplayIndex);
            
            return instance;
        }

        public T GetView<T>(string viewID, int targetDisplayIndex = 0) where T : UIView
        {
            return GetView(viewID, targetDisplayIndex) as T;
        }

        // ===================================================================================
        // 2. PUBLIC API - SHOW
        // ===================================================================================

        public async UniTask<UIView> Show(string viewID)
        {
            UIView view = GetView(viewID); // Defaults to existing display or 0
            return await Show(view);
        }

        public async UniTask<T> Show<T>(string viewID) where T : UIView
        {
            UIView view = GetView(viewID);
            await Show(view);
            return view as T;
        }

        public async UniTask<UIView> Show(UIView view)
        {
            if (view == null) return null;

            // Ensure it has a valid root (in case it was unparented)
            int displayIdx = GetDisplayIndexOfView(view);
            if (!_displays.TryGetValue(displayIdx, out var root))
            {
                SetViewToDisplay(view, 0); // Fallback to Main
                root = _displays[0];
            }

            // --- Stack Logic ---
            if (view.Type == ViewType.Modal)
            {
                if (!root.ModalStack.Contains(view))
                {
                    root.ModalStack.Push(view);
                    view.SetSortingOrder(ORDER_MODAL + (root.ModalStack.Count * 10));
                }
                UpdateCurtain(root, true, view.Canvas.sortingOrder - 1);
            }

            await view.Show();
            return view;
        }

        // ===================================================================================
        // 3. PUBLIC API - MANAGEMENT
        // ===================================================================================

        public void SetViewToDisplay(UIView view, int displayIndex)
        {
            if (view == null) return;

            // Auto-create display if missing
            if (!_displays.TryGetValue(displayIndex, out var root))
            {
                CreateDisplayRoot(displayIndex);
                root = _displays[displayIndex];
            }

            RectTransform targetLayer = null;
            int baseOrder = 0;

            switch (view.Type)
            {
                case ViewType.Background: targetLayer = root.BgRoot; baseOrder = ORDER_BG; break;
                case ViewType.Screen:     targetLayer = root.ScreenRoot; baseOrder = ORDER_SCREEN; break;
                case ViewType.Modal:      targetLayer = root.ModalRoot; baseOrder = ORDER_MODAL + (root.ModalStack.Count * 10); break;
                case ViewType.Widget:     targetLayer = root.WidgetRoot; baseOrder = ORDER_WIDGET + _activeWidgets.Count; break;
            }

            if (view.transform.parent != targetLayer)
            {
                view.transform.SetParent(targetLayer, false);
                ResetRectTransform(view.transform as RectTransform);
            }

            view.SetSortingOrder(baseOrder);
        }

        public async UniTask Hide(string viewID)
        {
            if (_viewCache.TryGetValue(viewID, out var view)) await Hide(view);
        }

        public async UniTask Hide(UIView view)
        {
            if (view == null) return;
            
            int displayIdx = GetDisplayIndexOfView(view);
            if (_displays.TryGetValue(displayIdx, out var root))
            {
                if (view.Type == ViewType.Modal && root.ModalStack.Count > 0 && root.ModalStack.Peek() == view)
                {
                    await CloseTopModal(root);
                    return;
                }
            }

            await view.Hide();
        }

        public async UniTask Back(int displayIndex = 0)
        {
            if (_displays.TryGetValue(displayIndex, out var root))
            {
                await CloseTopModal(root);
            }
        }

        // ===================================================================================
        // 4. DISPLAY FACTORY
        // ===================================================================================

        private void CreateDisplayRoot(int index)
        {
            if (_displays.ContainsKey(index)) return;

            // 1. Create Display Object under UIManager
            GameObject obj = new GameObject($"Display_{index + 1}_Root");
            obj.transform.SetParent(this.transform, false); 
            
            // 2. Add Canvas
            Canvas c = obj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.targetDisplay = index; // Unity Target Display
            
            CanvasScaler cs = obj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            
            obj.AddComponent<GraphicRaycaster>();

            // 3. Initialize Internal Layers
            InitializeLayers(index, c);
        }

        private void InitializeLayers(int index, Canvas canvas)
        {
            var root = new DisplayRoot { DisplayIndex = index, RootCanvas = canvas };

            root.BgRoot = CreateLayer(canvas.transform, "Layer_0_Background");
            root.ScreenRoot = CreateLayer(canvas.transform, "Layer_1_Screen");
            root.ModalRoot = CreateLayer(canvas.transform, "Layer_2_Modal");
            root.WidgetRoot = CreateLayer(canvas.transform, "Layer_3_Widget");

            // Per-Display Curtain
            root.ModalCurtain = CreateCurtain(root.ModalRoot);

            _displays[index] = root;
        }

        private RectTransform CreateLayer(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            ResetRectTransform(rect);
            
            // Ignore Layout is crucial to prevent layers from stacking horizontally/vertically
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
            
            return rect;
        }

        private CanvasGroup CreateCurtain(Transform parent)
        {
            GameObject obj = new GameObject("System_Curtain");
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            ResetRectTransform(rect);

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f);
            img.raycastTarget = true;

            CanvasGroup cg = obj.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            obj.SetActive(false);

            Canvas c = obj.AddComponent<Canvas>();
            c.overrideSorting = true;
            obj.AddComponent<GraphicRaycaster>();

            return cg;
        }

        // ===================================================================================
        // HELPERS
        // ===================================================================================

        private async UniTask CloseTopModal(DisplayRoot root)
        {
            if (root.ModalStack.Count == 0) return;

            UIView top = root.ModalStack.Pop();
            await top.Hide();

            if (root.ModalStack.Count > 0)
            {
                UIView next = root.ModalStack.Peek();
                UpdateCurtain(root, true, next.Canvas.sortingOrder - 1);
            }
            else
            {
                UpdateCurtain(root, false, 0);
            }
        }

        private void UpdateCurtain(DisplayRoot root, bool active, int sortOrder)
        {
            if (root.ModalCurtain == null) return;
            root.ModalCurtain.gameObject.SetActive(active);
            root.ModalCurtain.alpha = active ? 1f : 0f;
            root.ModalCurtain.blocksRaycasts = active;
            if (active) root.ModalCurtain.GetComponent<Canvas>().sortingOrder = sortOrder;
        }

        private int GetDisplayIndexOfView(UIView view)
        {
            // Traverse up to find which Display Root this view belongs to
            Transform current = view.transform.parent;
            while (current != null)
            {
                if (current.GetComponent<Canvas>() != null && current.parent == this.transform)
                {
                    // Found a root canvas that is a child of UIManager
                    // Check our dictionary
                    foreach(var kvp in _displays)
                    {
                        if (kvp.Value.RootCanvas.transform == current) return kvp.Key;
                    }
                }
                current = current.parent;
            }
            return 0; // Default to Main
        }

        private void ResetRectTransform(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}