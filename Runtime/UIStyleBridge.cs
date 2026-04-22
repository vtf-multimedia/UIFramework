using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISystem
{
    /// <summary>
    /// Utility to map the design system (StyleState) directly to UI Toolkit VisualElement styles.
    /// handles conversion between procedural data and IStyle properties 1-to-1.
    /// </summary>
    public static class UIStyleBridge
    {
        public static void Apply(VisualElement element, StyleDefinition def)
        {
            if (element == null || def == null) return;
            
            // Apply each property only if it's defined (additive styling)
            if (StyleState.ParseColor(def.backgroundColor, out var bg)) element.style.backgroundColor = bg;
            if (StyleState.ParseColor(def.color, out var c)) element.style.color = c;

            // Shorthands
            if (StyleState.ParseColor(def.borderColor, out var bc)) 
                element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = bc;
            if (StyleState.ParseFloat(def.borderRadius, out var br)) 
                element.style.borderTopLeftRadius = element.style.borderTopRightRadius = element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = br;
            if (StyleState.ParseFloat(def.borderWidth, out var bw)) 
                element.style.borderTopWidth = element.style.borderBottomWidth = element.style.borderLeftWidth = element.style.borderRightWidth = bw;
            if (StyleState.ParseFloat(def.margin, out var m)) 
                element.style.marginTop = element.style.marginBottom = element.style.marginLeft = element.style.marginRight = m;
            if (StyleState.ParseFloat(def.padding, out var p)) 
                element.style.paddingTop = element.style.paddingBottom = element.style.paddingLeft = element.style.paddingRight = p;

            // Specific Overrides
            if (StyleState.ParseColor(def.borderTopColor, out var btc)) element.style.borderTopColor = btc;
            if (StyleState.ParseColor(def.borderBottomColor, out var bbc)) element.style.borderBottomColor = bbc;
            if (StyleState.ParseColor(def.borderLeftColor, out var blc)) element.style.borderLeftColor = blc;
            if (StyleState.ParseColor(def.borderRightColor, out var brc)) element.style.borderRightColor = brc;

            if (StyleState.ParseFloat(def.borderTopLeftRadius, out var btlr)) element.style.borderTopLeftRadius = btlr;
            if (StyleState.ParseFloat(def.borderTopRightRadius, out var btrr)) element.style.borderTopRightRadius = btrr;
            if (StyleState.ParseFloat(def.borderBottomLeftRadius, out var bblr)) element.style.borderBottomLeftRadius = bblr;
            if (StyleState.ParseFloat(def.borderBottomRightRadius, out var bbrr)) element.style.borderBottomRightRadius = bbrr;

            if (StyleState.ParseFloat(def.width, out var w)) element.style.width = w;
            if (StyleState.ParseFloat(def.height, out var h)) element.style.height = h;
            if (StyleState.ParseFloat(def.flexGrow, out var fg)) element.style.flexGrow = fg;
            if (StyleState.ParseFloat(def.flexShrink, out var fs)) element.style.flexShrink = fs;
            if (StyleState.ParseFloat(def.opacity, out var op)) element.style.opacity = op;
            if (StyleState.ParseFloat(def.fontSize, out var fz)) element.style.fontSize = fz;

            if (StyleState.ParseFloat(def.top, out var pTop)) element.style.top = pTop;
            if (StyleState.ParseFloat(def.bottom, out var pBottom)) element.style.bottom = pBottom;
            if (StyleState.ParseFloat(def.left, out var pLeft)) element.style.left = pLeft;
            if (StyleState.ParseFloat(def.right, out var pRight)) element.style.right = pRight;

            if (!string.IsNullOrEmpty(def.position) && Enum.TryParse<Position>(def.position, true, out var pos))
                element.style.position = pos;
            
            if (!string.IsNullOrEmpty(def.unityTextAlign) && Enum.TryParse<TextAnchor>(def.unityTextAlign, true, out var align))
                element.style.unityTextAlign = align;

            // Transitions
            if (StyleState.ParseFloat(def.transitionDuration, out var td)) 
                element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(td, TimeUnit.Second) });
            if (StyleState.ParseFloat(def.transitionDelay, out var tdl)) 
                element.style.transitionDelay = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(tdl, TimeUnit.Second) });
            if (!string.IsNullOrEmpty(def.transitionTimingFunction) && Enum.TryParse<EasingMode>(def.transitionTimingFunction, true, out var ease))
                element.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(ease) });

            // Background Image (StreamingAssets)
            if (!string.IsNullOrEmpty(def.backgroundImage))
            {
                ApplyBackgroundAsync(element, def.backgroundImage).Forget();
            }

            // Text Shadow
            if (StyleState.ParseShadow(def.textShadow, out var tsc, out var tso, out var tsb))
            {
                element.style.textShadow = new StyleTextShadow(new TextShadow {
                    offset = tso,
                    blurRadius = tsb,
                    color = tsc
                });
            }
        }

        private static async UniTaskVoid ApplyBackgroundAsync(VisualElement element, string path)
        {
            var texture = await TextureLoader.GetTextureAsync(path);
            if (texture != null && element != null)
            {
                element.style.backgroundImage = new StyleBackground(texture);
            }
        }

        public static void Apply(VisualElement element, StyleState s)
        {
            if (element == null) return;
            IStyle style = element.style;

            // 1. COLORS
            style.backgroundColor = s.backgroundColor;
            style.color = s.color;
            style.borderTopColor = s.borderTopColor;
            style.borderBottomColor = s.borderBottomColor;
            style.borderLeftColor = s.borderLeftColor;
            style.borderRightColor = s.borderRightColor;

            // 2. RADII
            style.borderTopLeftRadius = s.borderTopLeftRadius;
            style.borderTopRightRadius = s.borderTopRightRadius;
            style.borderBottomLeftRadius = s.borderBottomLeftRadius;
            style.borderBottomRightRadius = s.borderBottomRightRadius;

            // 3. WIDTHS / SIZES
            if (s.width >= 0) style.width = s.width;
            if (s.height >= 0) style.height = s.height;
            style.borderTopWidth = s.borderTopWidth;
            style.borderBottomWidth = s.borderBottomWidth;
            style.borderLeftWidth = s.borderLeftWidth;
            style.borderRightWidth = s.borderRightWidth;

            // 4. MARGINS
            style.marginTop = s.marginTop;
            style.marginBottom = s.marginBottom;
            style.marginLeft = s.marginLeft;
            style.marginRight = s.marginRight;

            // 5. PADDINGS
            style.paddingTop = s.paddingTop;
            style.paddingBottom = s.paddingBottom;
            style.paddingLeft = s.paddingLeft;
            style.paddingRight = s.paddingRight;

            // 6. FLEX
            style.flexGrow = s.flexGrow;
            style.flexShrink = s.flexShrink;

            // 7. TEXT
            if (s.fontSize > 0) style.fontSize = s.fontSize;
            style.letterSpacing = s.letterSpacing;

            // 8. TRANSFORM / MISC
            style.opacity = s.opacity;
            element.transform.scale = new Vector3(s.scale.x, s.scale.y, 1f);
            element.transform.rotation = Quaternion.Euler(s.rotation);

            // 9. POSITIONING
            style.position = s.position;
            if (!float.IsNaN(s.top)) style.top = s.top;
            if (!float.IsNaN(s.bottom)) style.bottom = s.bottom;
            if (!float.IsNaN(s.left)) style.left = s.left;
            if (!float.IsNaN(s.right)) style.right = s.right;

            // 10. BACKGROUND IMAGE
            if (!string.IsNullOrEmpty(s.backgroundImage))
            {
                // Only load if path changed to prevent animation spam
                var currentBg = element.style.backgroundImage.value;
                if (currentBg.texture == null || currentBg.texture.name != s.backgroundImage)
                {
                    ApplyBackgroundAsync(element, s.backgroundImage).Forget();
                }
            }

            // 11. TEXT SHADOW
            if (s.textShadowColor.a > 0)
            {
                style.textShadow = new StyleTextShadow(new TextShadow {
                    offset = s.textShadowOffset,
                    blurRadius = s.textShadowBlur,
                    color = s.textShadowColor
                });
            }
            else
            {
                style.textShadow = new StyleTextShadow(StyleKeyword.Null);
            }
        }
    }
}
