using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace UIFramework
{
    [Serializable]
    public class UIStyle
    {
        private readonly UIBase _owner;
        private Image _shadowLayer;
        private Material _bgMat;
        private Material _shadowMat;
        private RectTransform _rect;
        private LayoutElement _layout;
        private CanvasGroup _cg;
        private TextMeshProUGUI _text;
        private Image _baseImage; // The procedural BG layer
        private Sprite _bgSprite;
        private string _loadedBgPath;

        public StyleState CurrentState;
        public StyleState InitialInspectorState;
        public StyleState NormalState;
        private bool _isInitialized = false;

        public UIStyle(UIBase owner)
        {
            _owner = owner;
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _rect = _owner.GetComponent<RectTransform>();
            _layout = _owner.GetComponent<LayoutElement>();
            _cg = _owner.GetComponent<CanvasGroup>();
            _text = _owner.GetComponent<TextMeshProUGUI>();
            
            CaptureInitialState();
            _isInitialized = true;
        }

        public void CaptureInitialState()
        {
            InitialInspectorState = StyleState.Default;
            
            if (_rect) {
                InitialInspectorState.SizeDelta = _rect.sizeDelta;
                InitialInspectorState.Scale = _owner.transform.localScale;
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
                // If parent has a LayoutGroup, we don't apply positional/sizing/scale/rotation to the RectTransform
                // as the LayoutGroup should have control over these.
                bool isUnderLayoutGroup = _owner.transform.parent != null && _owner.transform.parent.GetComponent<LayoutGroup>() != null;
                
                if (!isUnderLayoutGroup)
                {
                    if (_rect.sizeDelta != s.SizeDelta) _rect.sizeDelta = s.SizeDelta;
                    if (_owner.transform.localScale.x != s.Scale.x || _owner.transform.localScale.y != s.Scale.y) 
                        _owner.transform.localScale = new Vector3(s.Scale.x, s.Scale.y, 1f);
                    if (_owner.transform.localEulerAngles != s.Rotation) _owner.transform.localEulerAngles = s.Rotation;
                }
            }

            if (_layout)
            {
                _layout.preferredWidth = s.PreferredWidth;
                _layout.preferredHeight = s.PreferredHeight;
                _layout.flexibleWidth = s.FlexibleWidth;
                _layout.flexibleHeight = s.FlexibleHeight;
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
            catch (System.Exception e)
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
                UnityEngine.Object.Destroy(_shadowLayer.gameObject);
                _shadowLayer = null;
                if (_shadowMat) UnityEngine.Object.Destroy(_shadowMat);
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

                Color tint = (_bgSprite != null) ? Color.white : s.BackgroundColor;
                UpdateMat(_baseImage, _bgMat, tint, s.Radius, s.BorderWidth, s.BorderColor, s);
                _bgMat.SetFloat("_EdgeSoftness", 1f);
                _bgMat.SetFloat("_Margin", 0f);
            } else if (_baseImage) {
                UnityEngine.Object.Destroy(_baseImage.gameObject);
                _baseImage = null;
                if (_bgMat) UnityEngine.Object.Destroy(_bgMat);
                _bgMat = null;
            }
        }

        private Image FindOrCreateLayer(string n)
        {
            var t = _owner.transform.Find(n);
            if (t != null) return t.GetComponent<Image>();

            var go = new GameObject(n);
            go.transform.SetParent(_owner.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = (n == "_ProceduralBG");
            return img;
        }

        private void UpdateMat(Image img, Material mat, Color col, float rad, float borderW, Color borderC, StyleState s) {
            if(!mat || !img) return;
            mat.SetColor("_Color", col); mat.SetFloat("_Radius", rad);
            mat.SetFloat("_BorderWidth", borderW); mat.SetColor("_BorderColor", borderC);
            
            // USE OWNER RECT for stability (the layer might not have updated its anchors yet)
            var r = _rect != null ? _rect.rect : img.rectTransform.rect;
            mat.SetFloat("_Width", r.width); mat.SetFloat("_Height", r.height);
        }
    }
}