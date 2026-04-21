using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using PrimeTween;

namespace UIFramework
{
    public class UIIdentity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [UnityEngine.Serialization.FormerlySerializedAs("ID")]
        [SerializeField] private string _id;

        [UnityEngine.Serialization.FormerlySerializedAs("Classes")]
        [SerializeField] private List<string> _classes = new();

        public string ID => _id;
        public List<string> Classes => _classes;

        public Action OnUpdateIdentity;

        // --- Styling Fields (Merged from UIStyle) ---
        private Image _shadowLayer;
        private Material _bgMat;
        private Material _shadowMat;
        private RectTransform _rect;
        private LayoutElement _layout;
        private CanvasGroup _cg;
        private TextMeshProUGUI _text;
        private Image _baseImage;
        private Sprite _bgSprite;
        private string _loadedBgPath;
        private Image _invisibleHitbox;

        public StyleState CurrentState { get; private set; }
        public StyleState InitialInspectorState;
        public StyleState NormalState { get; private set; }
        
        private AnimationDef _animationDef;
        private bool _isInitialized = false;

        // --- Interaction Tracking ---
        private bool _isHovered;
        private bool _isPressed;
        private bool _isSelected;
        private bool _isChecked;
        
        private Toggle _attachedToggle;
        private Tween _interactionTween;

        private void Awake()
        {
            Initialize();

            // Auto-detect interaction components
            _attachedToggle = GetComponent<Toggle>();
            if (_attachedToggle != null)
            {
                _attachedToggle.onValueChanged.AddListener(OnToggleValueChanged);
                _isChecked = _attachedToggle.isOn;
            }

            // Suppress standard Unity transitions to avoid fighting with our procedural animations
            var selectable = GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.transition = Selectable.Transition.None;

                // Disable the native background image so it doesn't overlap the procedural one
                var rootImage = GetComponent<Image>();
                if (rootImage != null)
                {
                    rootImage.enabled = false;
                }
            }

            RefreshStyle();
            OnUpdateIdentity += RefreshStyle;
        }

        private void OnEnable()
        {
            if (StyleManager.Instance != null)
                StyleManager.Instance.OnThemeChanged += RefreshStyle;
        }

        private void OnDisable()
        {
            if (StyleManager.Instance != null)
                StyleManager.Instance.OnThemeChanged -= RefreshStyle;
            
            if (_interactionTween.isAlive) _interactionTween.Stop();
        }

        private void OnDestroy()
        {
            OnUpdateIdentity -= RefreshStyle;
            if (_attachedToggle != null)
            {
                _attachedToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
            if (_interactionTween.isAlive) _interactionTween.Stop();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _rect = GetComponent<RectTransform>();
            _layout = GetComponent<LayoutElement>();
            _cg = GetComponent<CanvasGroup>();
            _text = GetComponent<TextMeshProUGUI>();
            
            CaptureInitialState();
            _isInitialized = true;
        }

        public void CaptureInitialState()
        {
            InitialInspectorState = StyleState.Default;
            
            if (_rect) {
                InitialInspectorState.SizeDelta = _rect.sizeDelta;
                InitialInspectorState.Scale = transform.localScale;
            }
            if (_layout) {
                InitialInspectorState.PreferredWidth = _layout.preferredWidth;
                InitialInspectorState.PreferredHeight = _layout.preferredHeight;
                InitialInspectorState.FlexibleWidth = _layout.flexibleWidth;
                InitialInspectorState.FlexibleHeight = _layout.flexibleHeight;
            }
            if (_text) {
                InitialInspectorState.TextColor = _text.color;
                InitialInspectorState.FontSize = _text.fontSize;
                InitialInspectorState.CharacterSpacing = _text.characterSpacing;
            }
            
            CurrentState = InitialInspectorState;
        }

        public void SetID(string id)
        {
            _id = id; 
            OnUpdateIdentity?.Invoke();
        }

        public void AddClass(string className)
        {
            _classes.Add(className);
            OnUpdateIdentity?.Invoke();
        }
        
        public void RemoveClass(string className)
        {
            _classes.Remove(className);
            OnUpdateIdentity?.Invoke();
        }

        public void ClearClasses()
        {
            _classes.Clear();
            OnUpdateIdentity?.Invoke();
        }

        // ===================================================================================
        // Styling Logic (Merged from UIStyle)
        // ===================================================================================

        public void RefreshStyle()
        {
            if (StyleManager.Instance == null) return;

            var elementStyle = StyleManager.Instance.Resolve(ID, Classes);
            if (elementStyle != null)
            {
                _animationDef = elementStyle.Animation;

                if (elementStyle.BaseStyle != null)
                {
                    ApplyDefinition(elementStyle.BaseStyle);
                }

                // Immediately re-evaluate state in case we were hovered/checked when theme changed
                EvaluateState(true);
            }
        }

        public void ApplyDefinition(StyleDefinition def)
        {
            if (!_isInitialized) Initialize();
            var mergedState = StyleState.Merge(InitialInspectorState, def);
            Apply(mergedState);
            NormalState = mergedState; 
        }

        public void Apply(StyleState s)
        {
            CurrentState = s;

            if (_rect)
            {
                bool isUnderLayoutGroup = transform.parent != null && transform.parent.GetComponent<LayoutGroup>() != null;
                
                if (!isUnderLayoutGroup)
                {
                    if (_rect.sizeDelta != s.SizeDelta) _rect.sizeDelta = s.SizeDelta;
                    if (transform.localScale.x != s.Scale.x || transform.localScale.y != s.Scale.y) 
                        transform.localScale = new Vector3(s.Scale.x, s.Scale.y, 1f);
                    if (transform.localEulerAngles != s.Rotation) transform.localEulerAngles = s.Rotation;
                }
            }

            if (_layout)
            {
                if (s.PreferredWidth >= 0) _layout.preferredWidth = s.PreferredWidth;
                if (s.PreferredHeight >= 0) _layout.preferredHeight = s.PreferredHeight;
                if (s.FlexibleWidth >= 0) _layout.flexibleWidth = s.FlexibleWidth;
                if (s.FlexibleHeight >= 0) _layout.flexibleHeight = s.FlexibleHeight;
            }

            if (_cg) _cg.alpha = s.Opacity;
            if (_text)
            {
                _text.color = s.TextColor;
                if (s.FontSize > 0) _text.fontSize = s.FontSize;
            }

            if (s.BackgroundImagePath != _loadedBgPath)
            {
                _loadedBgPath = s.BackgroundImagePath;
                _bgSprite = LoadSprite(_loadedBgPath);
            }

            UpdateProceduralLayers(s);
        }

        private Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning($"Background image not found at: {fullPath}");
                return null;
            }

            try
            {
                byte[] data = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply();
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading background image: {e.Message}");
            }
            return null;
        }

        private void UpdateProceduralLayers(StyleState s)
        {
            bool needShadow = s.ShadowColor.a > 0.001f;
            if (needShadow) {
                if (!_shadowLayer) _shadowLayer = FindOrCreateLayer("_ProceduralShadow");
                if (!_shadowLayer.gameObject.activeSelf) _shadowLayer.gameObject.SetActive(true);
                if (_shadowLayer.transform.GetSiblingIndex() != 0) _shadowLayer.transform.SetAsFirstSibling();
                
                if (!_shadowMat) _shadowMat = new Material(Shader.Find("UI/ProceduralLayer"));
                _shadowLayer.material = _shadowMat;

                UpdateMat(_shadowLayer, _shadowMat, s.ShadowColor, s.Radius, 0, Color.clear, s);
                _shadowMat.SetFloat("_EdgeSoftness", s.ShadowSoftness);
                _shadowMat.SetFloat("_Margin", s.ShadowSoftness);
                
                var rt = _shadowLayer.rectTransform;
                rt.anchoredPosition = s.ShadowOffset;
                float pad = s.ShadowSoftness;
                rt.offsetMin = new Vector2(-pad, -pad); rt.offsetMax = new Vector2(pad, pad);
            } else if (_shadowLayer) {
                Destroy(_shadowLayer.gameObject);
                _shadowLayer = null;
                if (_shadowMat) Destroy(_shadowMat);
                _shadowMat = null;
            }

            bool needBg = s.BackgroundColor.a > 0.001f || s.BorderWidth > 0 || !string.IsNullOrEmpty(s.BackgroundImagePath);
            if (needBg) {
                if (!_baseImage) _baseImage = FindOrCreateLayer("_ProceduralBG");
                if (!_baseImage.gameObject.activeSelf) _baseImage.gameObject.SetActive(true);
                
                int targetIndex = (_shadowLayer && _shadowLayer.gameObject.activeSelf) ? 1 : 0;
                if (_baseImage.transform.GetSiblingIndex() != targetIndex) _baseImage.transform.SetSiblingIndex(targetIndex);

                if (!_bgMat) _bgMat = new Material(Shader.Find("UI/ProceduralLayer"));
                _baseImage.material = _bgMat;

                _baseImage.sprite = _bgSprite;
                if (_bgSprite != null)
                {
                    _bgMat.SetTexture("_MainTex", _bgSprite.texture);
                }
                else
                {
                    _bgMat.SetTexture("_MainTex", Texture2D.whiteTexture);
                }

                Color tint = (s.BackgroundColor.a > 0.001f) ? s.BackgroundColor : ((_bgSprite != null) ? Color.white : s.BackgroundColor);
                UpdateMat(_baseImage, _bgMat, tint, s.Radius, s.BorderWidth, s.BorderColor, s);
                _bgMat.SetFloat("_EdgeSoftness", 1f);
                _bgMat.SetFloat("_Margin", 0f);
                
                _baseImage.color = tint;
                _baseImage.SetMaterialDirty();
            } else if (_baseImage) {
                Destroy(_baseImage.gameObject);
                _baseImage = null;
                if (_bgMat) Destroy(_bgMat);
                _bgMat = null;
            }

            // Ensure Hitbox and bind Selectable
            var selectable = GetComponent<Selectable>();
            if (selectable != null)
            {
                if (needBg && _baseImage != null)
                {
                    selectable.targetGraphic = _baseImage;
                    if (_invisibleHitbox)
                    {
                        Destroy(_invisibleHitbox.gameObject);
                        _invisibleHitbox = null;
                    }
                }
                else
                {
                    if (!_invisibleHitbox)
                    {
                        var go = new GameObject("_InvisibleHitbox");
                        go.transform.SetParent(transform, false);
                        go.transform.SetAsFirstSibling();
                        var rt = go.AddComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.sizeDelta = Vector2.zero;
                        _invisibleHitbox = go.AddComponent<Image>();
                        _invisibleHitbox.color = Color.clear;
                        _invisibleHitbox.raycastTarget = true;
                    }
                    selectable.targetGraphic = _invisibleHitbox;
                }
            }
        }

        private Image FindOrCreateLayer(string n)
        {
            var t = transform.Find(n);
            if (t != null) return t.GetComponent<Image>();

            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = (n == "_ProceduralBG");
            return img;
        }

        private void UpdateMat(Image img, Material mat, Color col, float rad, float borderW, Color borderC, StyleState s) {
            if(!mat || !img || !img.rectTransform) return;
            mat.SetColor("_Color", col); mat.SetFloat("_Radius", rad);
            mat.SetFloat("_BorderWidth", borderW); mat.SetColor("_BorderColor", borderC);
            
            var r = _rect != null ? _rect.rect : img.rectTransform.rect;
            mat.SetFloat("_Width", r.width); mat.SetFloat("_Height", r.height);
        }

        // ===================================================================================
        // Native Interaction Engine
        // ===================================================================================

        public void OnPointerEnter(PointerEventData eventData) { _isHovered = true; EvaluateState(); }
        public void OnPointerExit(PointerEventData eventData) { _isHovered = false; EvaluateState(); }
        public void OnPointerDown(PointerEventData eventData) { _isPressed = true; EvaluateState(); }
        public void OnPointerUp(PointerEventData eventData) { _isPressed = false; EvaluateState(); }
        public void OnSelect(BaseEventData eventData) { _isSelected = true; EvaluateState(); }
        public void OnDeselect(BaseEventData eventData) { _isSelected = false; EvaluateState(); }
        
        private void OnToggleValueChanged(bool isOn) { _isChecked = isOn; EvaluateState(); }

        private void EvaluateState(bool instant = false)
        {
            if (_animationDef == null) return;

            StyleState targetVisualState = NormalState;
            TransitionDef transition = _animationDef.Transition; // Default transition

            // Priority: Check > Press > Hover > Select > Normal
            if (_isChecked && _animationDef.Check != null)
            {
                targetVisualState = StyleState.Merge(NormalState, _animationDef.Check.Style);
                if (_animationDef.Check.Transition != null && _animationDef.Check.Transition.duration > 0) transition = _animationDef.Check.Transition;
            }
            else if (_isPressed && _animationDef.Press != null)
            {
                targetVisualState = StyleState.Merge(NormalState, _animationDef.Press.Style);
                if (_animationDef.Press.Transition != null && _animationDef.Press.Transition.duration > 0) transition = _animationDef.Press.Transition;
            }
            else if (_isHovered && _animationDef.Hover != null)
            {
                targetVisualState = StyleState.Merge(NormalState, _animationDef.Hover.Style);
                if (_animationDef.Hover.Transition != null && _animationDef.Hover.Transition.duration > 0) transition = _animationDef.Hover.Transition;
            }
            else if (_isSelected && _animationDef.Select != null)
            {
                targetVisualState = StyleState.Merge(NormalState, _animationDef.Select.Style);
                if (_animationDef.Select.Transition != null && _animationDef.Select.Transition.duration > 0) transition = _animationDef.Select.Transition;
            }

            if (instant)
            {
                if (_interactionTween.isAlive) _interactionTween.Stop();
                Apply(targetVisualState);
                return;
            }

            // Tween to Target
            if (_interactionTween.isAlive) _interactionTween.Stop();

            StyleState start = CurrentState;
            float duration = transition?.duration ?? 0.15f;
            Ease ease = transition?.ease ?? Ease.Linear;

            if (duration <= 0.001f)
            {
                Apply(targetVisualState);
                return;
            }

            _interactionTween = Tween.Custom(0f, 1f, duration: duration, ease: ease, onValueChange: t =>
            {
                Apply(StyleState.Lerp(start, targetVisualState, t));
            });
        }
    }
}