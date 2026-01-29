using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIFramework
{
    [ExecuteAlways]
    public class UIStyle : MonoBehaviour
    {
        // ... (Variables for Layers/References same as before) ...
        [SerializeField] private Image _bgLayer;
        [SerializeField] private Image _shadowLayer;
        private Material _bgMat;
        private Material _shadowMat;
        private RectTransform _rect;
        private LayoutElement _layout;
        private CanvasGroup _cg;
        private TextMeshProUGUI _text;
        private Image _baseImage;

        
        public StyleState CurrentState;
        public StyleState InitialInspectorState;
        public StyleState NormalState;
        private bool _isInitialized = false;

        private void Awake() { Initialize(); }

        public void Initialize()
        {
            if (_isInitialized) return;
            _rect = GetComponent<RectTransform>();
            _layout = GetComponent<LayoutElement>();
            _cg = GetComponent<CanvasGroup>();
            _text = GetComponentInChildren<TextMeshProUGUI>();
            _baseImage = GetComponent<Image>();
            
            CaptureInitialState();
            _isInitialized = true;
        }

        public void CaptureInitialState()
        {
            InitialInspectorState = StyleState.Default;
            
            if (_rect) {
                InitialInspectorState.AnchoredPosition = _rect.anchoredPosition;
                InitialInspectorState.SizeDelta = _rect.sizeDelta;
                InitialInspectorState.Scale = transform.localScale;
                InitialInspectorState.AnchorMin = _rect.anchorMin;
                InitialInspectorState.AnchorMax = _rect.anchorMax;
                InitialInspectorState.Pivot = _rect.pivot;
            }
            if (_layout) {
                InitialInspectorState.PreferredWidth = _layout.preferredWidth;
                InitialInspectorState.PreferredHeight = _layout.preferredHeight;
                InitialInspectorState.FlexibleWidth = _layout.flexibleWidth;
                InitialInspectorState.FlexibleHeight = _layout.flexibleHeight;
            }
            if (_baseImage) InitialInspectorState.BackgroundColor = _baseImage.color;
            if (_text) {
                InitialInspectorState.TextColor = _text.color;
                InitialInspectorState.FontSize = _text.fontSize;
                InitialInspectorState.CharacterSpacing = _text.characterSpacing;
            }
            
            CurrentState = InitialInspectorState;
        }

        public void ApplyDefinition(StyleDefinition def)
        {
            if (!_isInitialized) Initialize();

            // 1. Merge the definition onto our clean Inspector State
            // This ensures we get ALL properties (Border, Shadow, Text, Rect, etc.)
            var mergedState = StyleState.Merge(InitialInspectorState, def);
            
            // 2. Apply it to the Unity Components
            Apply(mergedState);
            
            // 3. Update the baseline so animations start from here
            NormalState = mergedState; 
        }

        public void Apply(StyleState s)
        {
            CurrentState = s;

            // Rect
            if (_rect) {
                if (_rect.anchoredPosition != s.AnchoredPosition) _rect.anchoredPosition = s.AnchoredPosition;
                if (_rect.sizeDelta != s.SizeDelta) _rect.sizeDelta = s.SizeDelta;
                if (_rect.anchorMin != s.AnchorMin) _rect.anchorMin = s.AnchorMin;
                if (_rect.anchorMax != s.AnchorMax) _rect.anchorMax = s.AnchorMax;
                if (_rect.pivot != s.Pivot) _rect.pivot = s.Pivot;
                if (transform.localScale.x != s.Scale.x) transform.localScale = s.Scale;
                if (transform.localEulerAngles != s.Rotation) transform.localEulerAngles = s.Rotation;
            }

            // Layout
            if (_layout) {
                _layout.preferredWidth = s.PreferredWidth;
                _layout.preferredHeight = s.PreferredHeight;
                _layout.flexibleWidth = s.FlexibleWidth;
                _layout.flexibleHeight = s.FlexibleHeight;
            }

            // Visuals
            if (_cg) _cg.alpha = s.Opacity;
            if (_text) {
                _text.color = s.TextColor;
                if (s.FontSize > 0) _text.fontSize = s.FontSize;
                _text.characterSpacing = s.CharacterSpacing;
            }

            UpdateProceduralLayers(s);
        }

        // ... (UpdateProceduralLayers / CreateLayers methods remain the same) ...
        // ... (UpdateMat helper remains the same) ...
        
        private void UpdateProceduralLayers(StyleState s)
        {
            // (Same implementation as previous step, ensuring properties like s.Radius, s.BorderWidth are used)
            // ...
             // 1. Shadow (Index 0)
            bool needShadow = s.ShadowColor.a > 0.001f;
            if (needShadow) {
                if (!_shadowLayer) CreateShadowLayer();
                if (!_shadowLayer.gameObject.activeSelf) _shadowLayer.gameObject.SetActive(true);
                if (_shadowLayer.transform.GetSiblingIndex() != 0) _shadowLayer.transform.SetAsFirstSibling();
                
                UpdateMat(_shadowLayer, _shadowMat, s.ShadowColor, s.Radius, 0, Color.clear, s);
                _shadowMat.SetFloat("_EdgeSoftness", s.ShadowSoftness);
                _shadowMat.SetFloat("_Margin", s.ShadowSoftness);
                
                var rt = _shadowLayer.rectTransform;
                rt.anchoredPosition = s.ShadowOffset;
                float pad = s.ShadowSoftness;
                rt.offsetMin = new Vector2(-pad, -pad); rt.offsetMax = new Vector2(pad, pad);
            } else if (_shadowLayer && _shadowLayer.gameObject.activeSelf) _shadowLayer.gameObject.SetActive(false);

            // 2. BG (Index 1)
            bool needBg = s.BackgroundColor.a > 0.001f || s.BorderWidth > 0;
            if (needBg) {
                if (!_bgLayer) CreateBgLayer();
                if (!_bgLayer.gameObject.activeSelf) _bgLayer.gameObject.SetActive(true);
                
                int targetIndex = (_shadowLayer && _shadowLayer.gameObject.activeSelf) ? 1 : 0;
                if (_bgLayer.transform.GetSiblingIndex() != targetIndex) _bgLayer.transform.SetSiblingIndex(targetIndex);

                UpdateMat(_bgLayer, _bgMat, s.BackgroundColor, s.Radius, s.BorderWidth, s.BorderColor, s);
                if (_baseImage && _baseImage.enabled) _baseImage.enabled = false;
            } else {
                if (_bgLayer && _bgLayer.gameObject.activeSelf) _bgLayer.gameObject.SetActive(false);
                if (_baseImage && !_baseImage.enabled) _baseImage.enabled = true;
            }
        }
        
        // ...
        private void CreateShadowLayer() {
             _shadowLayer = CreateLayer("_ProceduralShadow"); _shadowLayer.transform.SetAsFirstSibling();
             _shadowLayer.raycastTarget = false;
             _shadowMat = new Material(Shader.Find("UI/ProceduralLayer")); _shadowLayer.material = _shadowMat;
        }
        private void CreateBgLayer() {
             _bgLayer = CreateLayer("_ProceduralBG");
             if(_shadowLayer) _bgLayer.transform.SetSiblingIndex(1); else _bgLayer.transform.SetAsFirstSibling();
             _bgLayer.raycastTarget = true;
             _bgMat = new Material(Shader.Find("UI/ProceduralLayer")); _bgLayer.material = _bgMat;
        }
        private Image CreateLayer(string n) {
             var go = new GameObject(n); go.transform.SetParent(transform, false);
             var rt = go.AddComponent<RectTransform>(); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.sizeDelta=Vector2.zero;
             return go.AddComponent<Image>();
        }
        private void UpdateMat(Image img, Material mat, Color col, float rad, float borderW, Color borderC, StyleState s) {
            if(!mat) return;
            mat.SetColor("_Color", col); mat.SetFloat("_Radius", rad);
            mat.SetFloat("_BorderWidth", borderW); mat.SetColor("_BorderColor", borderC);
            var r = img.rectTransform.rect; mat.SetFloat("_Width", r.width); mat.SetFloat("_Height", r.height);
        }
    }
}