using System;
using UnityEngine;
using PrimeTween; 
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UIFramework
{
    // --- 1. VISUAL DEFINITIONS ---
    [Serializable]
    public class StyleDefinition
    {
        public RectDef? rect;
        public LayoutItemDef? layoutItem;
        
        public string backgroundColor;
        public float? opacity;
        public float? radius;
        public BorderDef? border;
        public ShadowDef? shadow;
        
        public string textColor;
        public float? fontSize;
        public float? characterSpacing;
        
        public Vector2? scale;
        public Vector3? rotation;
    }

    // --- 2. TRANSITION CONFIG (Timing) ---
    [Serializable]
    public class TransitionDef
    {
        public float duration = 0.2f;
        public float delay = 0f;
        public Ease ease = Ease.Linear;
    }

    [Serializable]
    public class RepeatDef
    {
        // -1 = Infinite, 0/1 = Once. 
        public int cycles = 1; 
        public CycleMode cycleMode = CycleMode.Restart;
    }

    // --- 3. ANIMATION CONFIG ---
    [Serializable]
    public class AnimationDef
    {
        // Global timing settings for this element
        public TransitionDef Transition = new TransitionDef();
        public RepeatDef Repeat = new RepeatDef();

        // States (Visual Targets)
        // public StyleDefinition Initial;
        // public StyleDefinition Enter;
        // public StyleDefinition Exit;    // Target state for Hide()
        // public StyleDefinition Hover;
        // public StyleDefinition Press;
        // public StyleDefinition Animate; // Target state for Idle Loop
        // public StyleDefinition Check;

        public StateDef Initial;
        public StateDef Enter;
        public StateDef Exit;    // Target state for Hide()
        public StateDef Hover;
        public StateDef Press;
        public StateDef Animate; // Target state for Idle Loop
        public StateDef Check;
        
    }

    [Serializable]
    public class StateDef
    {
        public StyleDefinition Style = new();
        public TransitionDef Transition = new();
    }

    // --- 4. RUNTIME WRAPPER ---
    public class UIElementStyle
    {
        public StyleDefinition BaseStyle;
        public AnimationDef Animation;
        public Dictionary<string, UIElementStyle> Children = new Dictionary<string, UIElementStyle>();
    }

    // --- 5. CONCRETE STATE (Interpolatable) ---
    [Serializable]
    public struct StyleState
    {
        // Visuals
        public Color BackgroundColor; 
        public float Radius; 
        public float Opacity; 
        public float BorderWidth; 
        public Color BorderColor;
        
        public Color ShadowColor; 
        public Vector2 ShadowOffset; 
        public float ShadowSoftness;

        // Text
        public Color TextColor; 
        public float FontSize; 
        public float CharacterSpacing;

        // Transform / Rect
        public Vector2 Scale; 
        public Vector3 Rotation;
        public Vector2 AnchoredPosition; 
        public Vector2 SizeDelta;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;

        // Layout
        public float PreferredWidth; public float PreferredHeight;
        public float FlexibleWidth; public float FlexibleHeight;

        public static StyleState Default => new StyleState {
            BackgroundColor = Color.clear, BorderColor = Color.clear, TextColor = Color.black,
            Scale = Vector2.one, Opacity = 1f, FontSize = 14f, ShadowSoftness = 1f,
            PreferredWidth = -1, PreferredHeight = -1, FlexibleWidth = -1, FlexibleHeight = -1,
            AnchorMin = new Vector2(0.5f, 0.5f), AnchorMax = new Vector2(0.5f, 0.5f), Pivot = new Vector2(0.5f, 0.5f)
        };

        // --- THE MISSING PIECE: CENTRALIZED MERGE ---
        public static StyleState Merge(StyleState s, StyleDefinition def)
        {
            if (def == null) return s;

            // 1. Visuals
            if (ParseColor(def.backgroundColor, out var bg)) s.BackgroundColor = bg;
            if (def.opacity.HasValue) s.Opacity = def.opacity.Value;
            if (def.radius.HasValue) s.Radius = def.radius.Value;
            
            // 2. Border
            if (def.border.HasValue) {
                if (def.border.Value.width.HasValue) s.BorderWidth = def.border.Value.width.Value;
                if (ParseColor(def.border.Value.color, out var bc)) s.BorderColor = bc;
            }

            // 3. Shadow
            if (def.shadow.HasValue) {
                var sh = def.shadow.Value;
                if (ParseColor(sh.color, out var sc)) s.ShadowColor = sc;
                if (sh.x.HasValue && sh.y.HasValue) s.ShadowOffset = new Vector2(sh.x.Value, sh.y.Value);
                if (sh.softness.HasValue) s.ShadowSoftness = sh.softness.Value;
            }

            // 4. Text
            if (ParseColor(def.textColor, out var tc)) s.TextColor = tc;
            if (def.fontSize.HasValue) s.FontSize = def.fontSize.Value;
            if (def.characterSpacing.HasValue) s.CharacterSpacing = def.characterSpacing.Value;

            // 5. Transform / Rect
            if (def.scale.HasValue) s.Scale = def.scale.Value;
            if (def.rotation.HasValue) s.Rotation = def.rotation.Value;
            
            if (def.rect.HasValue) {
                var r = def.rect.Value;
                if (r.anchoredPosition.HasValue) s.AnchoredPosition = r.anchoredPosition.Value;
                if (r.sizeDelta.HasValue) s.SizeDelta = r.sizeDelta.Value;
                if (r.anchorMin.HasValue) s.AnchorMin = r.anchorMin.Value;
                if (r.anchorMax.HasValue) s.AnchorMax = r.anchorMax.Value;
                if (r.pivot.HasValue) s.Pivot = r.pivot.Value;
            }

            // 6. Layout
            if (def.layoutItem.HasValue) {
                var l = def.layoutItem.Value;
                if (l.preferredWidth.HasValue) s.PreferredWidth = l.preferredWidth.Value;
                if (l.preferredHeight.HasValue) s.PreferredHeight = l.preferredHeight.Value;
                if (l.flexibleWidth.HasValue) s.FlexibleWidth = l.flexibleWidth.Value;
                if (l.flexibleHeight.HasValue) s.FlexibleHeight = l.flexibleHeight.Value;
            }

            return s;
        }

        // --- LERP (For Animation) ---
        public static StyleState Lerp(StyleState a, StyleState b, float t)
        {
            var r = new StyleState();
            // Visuals
            r.BackgroundColor = Color.Lerp(a.BackgroundColor, b.BackgroundColor, t);
            r.Radius = Mathf.Lerp(a.Radius, b.Radius, t);
            r.Opacity = Mathf.Lerp(a.Opacity, b.Opacity, t);
            
            // Border & Shadow
            r.BorderWidth = Mathf.Lerp(a.BorderWidth, b.BorderWidth, t);
            r.BorderColor = Color.Lerp(a.BorderColor, b.BorderColor, t);
            r.ShadowColor = Color.Lerp(a.ShadowColor, b.ShadowColor, t);
            r.ShadowOffset = Vector2.Lerp(a.ShadowOffset, b.ShadowOffset, t);
            r.ShadowSoftness = Mathf.Lerp(a.ShadowSoftness, b.ShadowSoftness, t);

            // Text
            r.TextColor = Color.Lerp(a.TextColor, b.TextColor, t);
            r.FontSize = Mathf.Lerp(a.FontSize, b.FontSize, t);
            r.CharacterSpacing = Mathf.Lerp(a.CharacterSpacing, b.CharacterSpacing, t);

            // Transform
            r.Scale = Vector2.Lerp(a.Scale, b.Scale, t);
            r.Rotation = Vector3.Lerp(a.Rotation, b.Rotation, t);
            
            // Rect
            r.AnchoredPosition = Vector2.Lerp(a.AnchoredPosition, b.AnchoredPosition, t);
            r.SizeDelta = Vector2.Lerp(a.SizeDelta, b.SizeDelta, t);
            r.AnchorMin = Vector2.Lerp(a.AnchorMin, b.AnchorMin, t);
            r.AnchorMax = Vector2.Lerp(a.AnchorMax, b.AnchorMax, t);
            r.Pivot = Vector2.Lerp(a.Pivot, b.Pivot, t);

            // Layout
            r.PreferredWidth = Mathf.Lerp(a.PreferredWidth, b.PreferredWidth, t);
            r.PreferredHeight = Mathf.Lerp(a.PreferredHeight, b.PreferredHeight, t);
            r.FlexibleWidth = Mathf.Lerp(a.FlexibleWidth, b.FlexibleWidth, t);
            r.FlexibleHeight = Mathf.Lerp(a.FlexibleHeight, b.FlexibleHeight, t);
            
            return r;
        }

        private static bool ParseColor(string h, out Color c) { c = Color.white; return !string.IsNullOrEmpty(h) && ColorUtility.TryParseHtmlString(h, out c); }
    }

    [Serializable] 
    public struct RectDef 
    { 
        public Vector2? anchoredPosition; 
        public Vector2? sizeDelta;
        public Vector2? anchorMin;
        public Vector2? anchorMax;
        public Vector2? pivot;
    }
    [Serializable] public struct LayoutItemDef { public float? preferredWidth; public float? preferredHeight; public float? flexibleWidth; public float? flexibleHeight; }
    [Serializable] public struct BorderDef { public float? width; public string color; }
    [Serializable] public struct ShadowDef { public float? x; public float? y; public string color; public float? softness; }
}