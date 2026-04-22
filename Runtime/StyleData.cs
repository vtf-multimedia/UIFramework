using System;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace UISystem
{
    public class ThemeData
    {
        public Dictionary<string, string> variables = new();
        public Dictionary<string, UIElementStyle> styles = new();
    }

    // --- 1. VISUAL DEFINITIONS (Targeting IStyle 1-to-1) ---
    [Serializable]
    public class StyleDefinition
    {
        // Shorthands (New)
        public string borderColor;
        public string borderRadius;
        public string borderWidth;
        public string margin;
        public string padding;

        // Transitions
        public string transitionDuration;
        public string transitionDelay;
        public string transitionTimingFunction;

        // Colors
        public string backgroundColor;
        public string color;
        public string borderTopColor;
        public string borderBottomColor;
        public string borderLeftColor;
        public string borderRightColor;

        // Radii
        public string borderTopLeftRadius;
        public string borderTopRightRadius;
        public string borderBottomLeftRadius;
        public string borderBottomRightRadius;

        // Widths / Sizes
        public string width;
        public string height;
        public string borderTopWidth;
        public string borderBottomWidth;
        public string borderLeftWidth;
        public string borderRightWidth;

        // Margins
        public string marginTop;
        public string marginBottom;
        public string marginLeft;
        public string marginRight;

        // Paddings
        public string paddingTop;
        public string paddingBottom;
        public string paddingLeft;
        public string paddingRight;

        // Flex
        public string flexGrow;
        public string flexShrink;
        
        // Text
        public string fontSize;
        public string letterSpacing;
        public string unityTextAlign;

        // Transform / Misc
        public string opacity;
        public Vector2? scale; // Keeping Vector2/3 as they are rarely variables, but could be strings too
        public Vector3? rotation;
        
        // Position
        public string position; // absolute, relative
        public string top;
        public string bottom;
        public string left;
        public string right;

        // Background
        public string backgroundImage;

        // Shadows
        public string shadow;
        public string textShadow;
    }

    [Serializable]
    public class TransitionDef
    {
        public float duration = 0.2f;
        public float delay = 0f;
        public string ease = "Linear";
    }

    [Serializable]
    public class AnimationDef
    {
        public TransitionDef transition = new TransitionDef();
        public StateDef initial;
        public StateDef enter;
        public StateDef exit;
        public StateDef hover;
        public StateDef press;
        public StateDef check;
        public StateDef select;
    }

    [Serializable]
    public class StateDef
    {
        public StyleDefinition style = new();
        public TransitionDef transition = new();
    }

    public class UIElementStyle
    {
        public StyleDefinition baseStyle;
        public AnimationDef animation;
        public Dictionary<string, UIElementStyle> children = new Dictionary<string, UIElementStyle>();
    }

    // --- 5. CONCRETE STATE (Used for Interpolation/Applying) ---
    [Serializable]
    public struct StyleState
    {
        public Color backgroundColor;
        public Color color;
        public Color borderTopColor;
        public Color borderBottomColor;
        public Color borderLeftColor;
        public Color borderRightColor;

        public float borderTopLeftRadius;
        public float borderTopRightRadius;
        public float borderBottomLeftRadius;
        public float borderBottomRightRadius;

        public float width;
        public float height;
        public float borderTopWidth;
        public float borderBottomWidth;
        public float borderLeftWidth;
        public float borderRightWidth;

        public float marginTop;
        public float marginBottom;
        public float marginLeft;
        public float marginRight;

        public float paddingTop;
        public float paddingBottom;
        public float paddingLeft;
        public float paddingRight;

        public float flexGrow;
        public float flexShrink;

        public float fontSize;
        public float letterSpacing;
        
        public float opacity;
        public Vector2 scale;
        public Vector3 rotation;
        
        public float top;
        public float bottom;
        public float left;
        public float right;
        public Position position;

        public string backgroundImage;

        public Color shadowColor;
        public Vector2 shadowOffset;
        public float shadowBlur;

        public Color textShadowColor;
        public Vector2 textShadowOffset;
        public float textShadowBlur;

        public static StyleState Default => new StyleState {
            backgroundColor = Color.clear,
            color = Color.white,
            scale = Vector2.one,
            opacity = 1f,
            fontSize = 14f,
            width = -1,
            height = -1,
            top = float.NaN,
            bottom = float.NaN,
            left = float.NaN,
            right = float.NaN,
            position = Position.Relative,
            shadowColor = Color.clear,
            textShadowColor = Color.clear
        };

        public static StyleState Merge(StyleState s, StyleDefinition def)
        {
            if (def == null) return s;

            // 1. SHORTHANDS (Applied first, then overridden by specific fields)
            if (ParseColor(def.borderColor, out var bc)) 
            {
                s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = bc;
            }
            if (ParseFloat(def.borderRadius, out var br))
            {
                s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = br;
            }
            if (ParseFloat(def.borderWidth, out var bw))
            {
                s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = bw;
            }
            if (ParseFloat(def.margin, out var m))
            {
                s.marginTop = s.marginBottom = s.marginLeft = s.marginRight = m;
            }
            if (ParseFloat(def.padding, out var p))
            {
                s.paddingTop = s.paddingBottom = s.paddingLeft = s.paddingRight = p;
            }

            // 2. SPECIFIC OVERRIDES
            // Colors
            if (ParseColor(def.backgroundColor, out var bg)) s.backgroundColor = bg;
            if (ParseColor(def.color, out var c)) s.color = c;
            if (ParseColor(def.borderTopColor, out var btc)) s.borderTopColor = btc;
            if (ParseColor(def.borderBottomColor, out var bbc)) s.borderBottomColor = bbc;
            if (ParseColor(def.borderLeftColor, out var blc)) s.borderLeftColor = blc;
            if (ParseColor(def.borderRightColor, out var brc)) s.borderRightColor = brc;

            // Radii
            if (ParseFloat(def.borderTopLeftRadius, out var btlr)) s.borderTopLeftRadius = btlr;
            if (ParseFloat(def.borderTopRightRadius, out var btrr)) s.borderTopRightRadius = btrr;
            if (ParseFloat(def.borderBottomLeftRadius, out var bblr)) s.borderBottomLeftRadius = bblr;
            if (ParseFloat(def.borderBottomRightRadius, out var bbrr)) s.borderBottomRightRadius = bbrr;

            // Sizes
            if (ParseFloat(def.width, out var w)) s.width = w;
            if (ParseFloat(def.height, out var h)) s.height = h;
            if (ParseFloat(def.borderTopWidth, out var btw)) s.borderTopWidth = btw;
            if (ParseFloat(def.borderBottomWidth, out var bbw)) s.borderBottomWidth = bbw;
            if (ParseFloat(def.borderLeftWidth, out var blw)) s.borderLeftWidth = blw;
            if (ParseFloat(def.borderRightWidth, out var brw)) s.borderRightWidth = brw;

            // Margins
            if (ParseFloat(def.marginTop, out var mt)) s.marginTop = mt;
            if (ParseFloat(def.marginBottom, out var mb)) s.marginBottom = mb;
            if (ParseFloat(def.marginLeft, out var ml)) s.marginLeft = ml;
            if (ParseFloat(def.marginRight, out var mr)) s.marginRight = mr;

            // Paddings
            if (ParseFloat(def.paddingTop, out var pt)) s.paddingTop = pt;
            if (ParseFloat(def.paddingBottom, out var pb)) s.paddingBottom = pb;
            if (ParseFloat(def.paddingLeft, out var pl)) s.paddingLeft = pl;
            if (ParseFloat(def.paddingRight, out var pr)) s.paddingRight = pr;

            // Flex
            if (ParseFloat(def.flexGrow, out var fg)) s.flexGrow = fg;
            if (ParseFloat(def.flexShrink, out var fs)) s.flexShrink = fs;

            // Text
            if (ParseFloat(def.fontSize, out var fsSize)) s.fontSize = fsSize;
            if (ParseFloat(def.letterSpacing, out var ls)) s.letterSpacing = ls;

            // Transform
            if (ParseFloat(def.opacity, out var o)) s.opacity = o;
            if (def.scale.HasValue) s.scale = def.scale.Value;
            if (def.rotation.HasValue) s.rotation = def.rotation.Value;

            // Position
            if (!string.IsNullOrEmpty(def.position) && Enum.TryParse<Position>(def.position, true, out var pos))
                s.position = pos;

            if (ParseFloat(def.top, out var pTop)) s.top = pTop;
            if (ParseFloat(def.bottom, out var pBottom)) s.bottom = pBottom;
            if (ParseFloat(def.left, out var pLeft)) s.left = pLeft;
            if (ParseFloat(def.right, out var pRight)) s.right = pRight;

            if (!string.IsNullOrEmpty(def.backgroundImage)) s.backgroundImage = def.backgroundImage;

            if (ParseShadow(def.shadow, out var sc, out var so, out var sb))
            {
                s.shadowColor = sc;
                s.shadowOffset = so;
                s.shadowBlur = sb;
            }

            if (ParseShadow(def.textShadow, out var tsc, out var tso, out var tsb))
            {
                s.textShadowColor = tsc;
                s.textShadowOffset = tso;
                s.textShadowBlur = tsb;
            }

            return s;
        }

        public static StyleState Lerp(StyleState a, StyleState b, float t)
        {
            var r = new StyleState();
            r.backgroundColor = Color.Lerp(a.backgroundColor, b.backgroundColor, t);
            r.color = Color.Lerp(a.color, b.color, t);
            r.borderTopColor = Color.Lerp(a.borderTopColor, b.borderTopColor, t);
            r.borderBottomColor = Color.Lerp(a.borderBottomColor, b.borderBottomColor, t);
            r.borderLeftColor = Color.Lerp(a.borderLeftColor, b.borderLeftColor, t);
            r.borderRightColor = Color.Lerp(a.borderRightColor, b.borderRightColor, t);

            r.borderTopLeftRadius = Mathf.Lerp(a.borderTopLeftRadius, b.borderTopLeftRadius, t);
            r.borderTopRightRadius = Mathf.Lerp(a.borderTopRightRadius, b.borderTopRightRadius, t);
            r.borderBottomLeftRadius = Mathf.Lerp(a.borderBottomLeftRadius, b.borderBottomLeftRadius, t);
            r.borderBottomRightRadius = Mathf.Lerp(a.borderBottomRightRadius, b.borderBottomRightRadius, t);

            r.width = Mathf.Lerp(a.width, b.width, t);
            r.height = Mathf.Lerp(a.height, b.height, t);
            r.borderTopWidth = Mathf.Lerp(a.borderTopWidth, b.borderTopWidth, t);
            r.borderBottomWidth = Mathf.Lerp(a.borderBottomWidth, b.borderBottomWidth, t);
            r.borderLeftWidth = Mathf.Lerp(a.borderLeftWidth, b.borderLeftWidth, t);
            r.borderRightWidth = Mathf.Lerp(a.borderRightWidth, b.borderRightWidth, t);

            r.marginTop = Mathf.Lerp(a.marginTop, b.marginTop, t);
            r.marginBottom = Mathf.Lerp(a.marginBottom, b.marginBottom, t);
            r.marginLeft = Mathf.Lerp(a.marginLeft, b.marginLeft, t);
            r.marginRight = Mathf.Lerp(a.marginRight, b.marginRight, t);

            r.paddingTop = Mathf.Lerp(a.paddingTop, b.paddingTop, t);
            r.paddingBottom = Mathf.Lerp(a.paddingBottom, b.paddingBottom, t);
            r.paddingLeft = Mathf.Lerp(a.paddingLeft, b.paddingLeft, t);
            r.paddingRight = Mathf.Lerp(a.paddingRight, b.paddingRight, t);

            r.flexGrow = Mathf.Lerp(a.flexGrow, b.flexGrow, t);
            r.flexShrink = Mathf.Lerp(a.flexShrink, b.flexShrink, t);

            r.fontSize = Mathf.Lerp(a.fontSize, b.fontSize, t);
            r.letterSpacing = Mathf.Lerp(a.letterSpacing, b.letterSpacing, t);

            r.opacity = Mathf.Lerp(a.opacity, b.opacity, t);
            r.scale = Vector2.Lerp(a.scale, b.scale, t);
            r.rotation = Vector3.Lerp(a.rotation, b.rotation, t);

            r.top = float.IsNaN(a.top) || float.IsNaN(b.top) ? b.top : Mathf.Lerp(a.top, b.top, t);
            r.bottom = float.IsNaN(a.bottom) || float.IsNaN(b.bottom) ? b.bottom : Mathf.Lerp(a.bottom, b.bottom, t);
            r.left = float.IsNaN(a.left) || float.IsNaN(b.left) ? b.left : Mathf.Lerp(a.left, b.left, t);
            r.right = float.IsNaN(a.right) || float.IsNaN(b.right) ? b.right : Mathf.Lerp(a.right, b.right, t);
            r.position = t > 0.5f ? b.position : a.position;

            r.backgroundImage = t > 0.5f ? b.backgroundImage : a.backgroundImage;

            r.shadowColor = Color.Lerp(a.shadowColor, b.shadowColor, t);
            r.shadowOffset = Vector2.Lerp(a.shadowOffset, b.shadowOffset, t);
            r.shadowBlur = Mathf.Lerp(a.shadowBlur, b.shadowBlur, t);

            r.textShadowColor = Color.Lerp(a.textShadowColor, b.textShadowColor, t);
            r.textShadowOffset = Vector2.Lerp(a.textShadowOffset, b.textShadowOffset, t);
            r.textShadowBlur = Mathf.Lerp(a.textShadowBlur, b.textShadowBlur, t);

            return r;
        }

        public static bool ResolveVariable(string input, out string resolved)
        {
            resolved = input;
            if (string.IsNullOrEmpty(input) || !input.Contains("var(")) return false;
            string val = StyleManager.Instance.GetVariable(input);
            if (!string.IsNullOrEmpty(val))
            {
                resolved = val;
                return true;
            }
            return false;
        }

        public static bool ParseColor(string h, out Color c) 
        { 
            c = Color.white; 
            if (string.IsNullOrEmpty(h)) return false; 
            ResolveVariable(h, out h);
            return ColorUtility.TryParseHtmlString(h, out c); 
        }

        public static bool ParseFloat(string s, out float f)
        {
            f = 0;
            if (string.IsNullOrEmpty(s)) return false;
            ResolveVariable(s, out s);
            s = s.Replace("px", "").Trim();
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
        }

        public static bool ParseShadow(string s, out Color color, out Vector2 offset, out float blur)
        {
            color = Color.clear;
            offset = Vector2.zero;
            blur = 0;

            if (string.IsNullOrEmpty(s)) return false;
            ResolveVariable(s, out s);

            // Format: "offsetX offsetY blur color" (e.g. "2px 2px 5px rgba(0,0,0,0.5)")
            string[] parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            int currentPart = 0;
            
            // X Offset
            if (currentPart < parts.Length && ParseFloat(parts[currentPart], out float x)) { offset.x = x; currentPart++; }
            // Y Offset
            if (currentPart < parts.Length && ParseFloat(parts[currentPart], out float y)) { offset.y = y; currentPart++; }
            // Blur (Optional in some CSS, but we'll try to pick it if there are 4 parts)
            if (parts.Length >= 4)
            {
                if (ParseFloat(parts[currentPart], out float b)) { blur = b; currentPart++; }
            }

            // The rest should be the color
            string colorPart = string.Join(" ", parts, currentPart, parts.Length - currentPart);
            if (ParseColor(colorPart, out Color c)) color = c;

            return true;
        }
    }
}