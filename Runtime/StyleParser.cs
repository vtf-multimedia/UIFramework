using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween; 

namespace UISystem
{
    public static class StyleParser
    {
        public static ThemeData Parse(string jsonContent)
        {
            var theme = new ThemeData();
            var root = JObject.Parse(jsonContent);

            // 1. Capture Variables
            if (root["variables"] is JObject varsObj)
            {
                foreach (var prop in varsObj.Properties())
                {
                    theme.variables[prop.Name] = prop.Value.ToString();
                }
            }

            // 2. Parse Styles
            if (root["styles"] is JObject stylesObj)
            {
                // We still support static resolution of $vars for backward compatibility or JSON-side logic
                ResolveStaticVariablesRecursive(stylesObj, theme.variables);
                
                foreach (var prop in stylesObj.Properties())
                {
                    if (prop.Value.Type == JTokenType.Object)
                        theme.styles[prop.Name] = ParseElement((JObject)prop.Value);
                }
            }
            return theme;
        }

        private static void ResolveStaticVariablesRecursive(JToken token, Dictionary<string, string> vars)
        {
            if (token.Type == JTokenType.Object) { foreach (var child in token.Children<JProperty>()) ResolveStaticVariablesRecursive(child.Value, vars); }
            else if (token.Type == JTokenType.Array) { foreach (var child in token.Children()) ResolveStaticVariablesRecursive(child, vars); }
            else if (token.Type == JTokenType.String)
            {
                string val = token.ToString();
                if (val.StartsWith("$") && val.Length > 1)
                {
                    if (vars.TryGetValue(val.Substring(1), out var resolved))
                    {
                        if (token.Parent is JProperty p) p.Value = resolved;
                        else if (token.Parent is JArray a) a[a.IndexOf(token)] = resolved;
                    }
                }
            }
        }

        private static UIElementStyle ParseElement(JObject jobj)
        {
            var element = new UIElementStyle();
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };
            
            element.baseStyle = jobj.ToObject<StyleDefinition>(JsonSerializer.Create(settings));
            element.animation = new AnimationDef();

            foreach (var prop in jobj.Properties())
            {
                if (prop.Value.Type != JTokenType.Object) continue;
                string key = prop.Name;
                JObject child = (JObject)prop.Value;

                if (key == "animation") ParseAnimationGroup(child, element.animation);
                else if (key.StartsWith(".") || key.StartsWith("#")) element.children[key] = ParseElement(child);
            }
            return element;
        }

        private static void ParseAnimationGroup(JObject obj, AnimationDef target)
        {
            var ser = JsonSerializer.Create(new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

            foreach (var prop in obj.Properties())
            {
                if (prop.Name == "transition" && prop.Value.Type == JTokenType.Object)
                {
                    target.transition = ParseTransition((JObject)prop.Value);
                    continue;
                }

                if (prop.Name == "repeat" && prop.Value.Type == JTokenType.Object)
                {
                    // Repeat logic not yet used in bridge, but parsed
                }

                if (prop.Value.Type != JTokenType.Object) continue;
                
                switch (prop.Name.ToLower())
                {
                    case "enter": target.enter = ParseState((JObject)prop.Value); break;
                    case "exit":    target.exit = ParseState((JObject)prop.Value); break;
                    case "initial": target.initial = ParseState((JObject)prop.Value); break;
                    case "hover":   target.hover = ParseState((JObject)prop.Value); break;
                    case "press":   target.press = ParseState((JObject)prop.Value); break;
                    case "check":  target.check = ParseState((JObject)prop.Value); break;
                    case "select": target.select = ParseState((JObject)prop.Value); break;
                }
            }
        }

        private static StateDef ParseState(JObject obj)
        {
            var s = new StateDef();
            s.style = obj.ToObject<StyleDefinition>();
            if (obj.TryGetValue("transition", out var value))
            {
                s.transition = value.ToObject<TransitionDef>();
            }
            return s;
        }

        private static TransitionDef ParseTransition(JObject obj)
        {
            var t = new TransitionDef();
            if (obj["duration"] != null) t.duration = (float)obj["duration"];
            if (obj["delay"] != null) t.delay = (float)obj["delay"];
            if (obj["ease"] != null) t.ease = obj["ease"].ToString();
            return t;
        }
    }
}