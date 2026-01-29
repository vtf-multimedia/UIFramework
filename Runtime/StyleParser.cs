using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween; 

namespace UIFramework
{
    public static class StyleParser
    {
        public static Dictionary<string, UIElementStyle> Parse(string jsonContent)
        {
            var result = new Dictionary<string, UIElementStyle>();
            var root = JObject.Parse(jsonContent);

            // 1. Resolve Variables
            var variables = new Dictionary<string, JToken>();
            if (root["variables"] is JObject varsObj)
            {
                foreach (var prop in varsObj.Properties()) variables[prop.Name] = prop.Value;
            }

            if (root["styles"] is JObject stylesObj)
            {
                ResolveVariablesRecursive(stylesObj, variables);
                foreach (var prop in stylesObj.Properties())
                {
                    if (prop.Value.Type == JTokenType.Object)
                        result[prop.Name] = ParseElement((JObject)prop.Value);
                }
            }
            return result;
        }

        private static void ResolveVariablesRecursive(JToken token, Dictionary<string, JToken> vars)
        {
            if (token.Type == JTokenType.Object) { foreach (var child in token.Children<JProperty>()) ResolveVariablesRecursive(child.Value, vars); }
            else if (token.Type == JTokenType.Array) { foreach (var child in token.Children()) ResolveVariablesRecursive(child, vars); }
            else if (token.Type == JTokenType.String)
            {
                string val = token.ToString();
                if (val.StartsWith("$") && val.Length > 1)
                {
                    if (vars.TryGetValue(val.Substring(1), out var resolved))
                    {
                        if (token.Parent is JProperty p) p.Value = resolved.DeepClone();
                        else if (token.Parent is JArray a) a[a.IndexOf(token)] = resolved.DeepClone();
                    }
                }
            }
        }

        private static UIElementStyle ParseElement(JObject jobj)
        {
            var element = new UIElementStyle();
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore };
            
            element.BaseStyle = jobj.ToObject<StyleDefinition>(JsonSerializer.Create(settings));
            element.Animation = new AnimationDef();

            foreach (var prop in jobj.Properties())
            {
                if (prop.Value.Type != JTokenType.Object) continue;
                string key = prop.Name;
                JObject child = (JObject)prop.Value;

                if (key == "animation") ParseAnimationGroup(child, element.Animation);
                else if (key.StartsWith(".") || key.StartsWith("#")) element.Children[key] = ParseElement(child);
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
                    target.Transition = ParseTransition((JObject)prop.Value);
                    continue;
                }

                if (prop.Name == "repeat" && prop.Value.Type == JTokenType.Object)
                {
                    target.Repeat = ParseRepeat((JObject)prop.Value);
                    continue;
                }

                if (prop.Value.Type != JTokenType.Object) continue;
                var style = prop.Value.ToObject<StyleDefinition>(ser);

                switch (prop.Name.ToLower())
                {
                    case "enter": target.Enter = ParseState((JObject)prop.Value); break;
                    case "exit":    target.Exit = ParseState((JObject)prop.Value); break;
                    case "initial": target.Initial = ParseState((JObject)prop.Value); break;
                    case "animate": target.Animate = ParseState((JObject)prop.Value); break;
                    case "hover":   target.Hover = ParseState((JObject)prop.Value); break;
                    case "press":   target.Press = ParseState((JObject)prop.Value); break;
                    case "check":  target.Check = ParseState((JObject)prop.Value); break;
                }
            }
        }


        private static StateDef ParseState(JObject obj)
        {
            var s = new StateDef();
            s.Style = obj.ToObject<StyleDefinition>();
            if (obj.TryGetValue("transition", out var value))
            {
                s.Transition = value.ToObject<TransitionDef>();
            }

            return s;
        }
        

        private static TransitionDef ParseTransition(JObject obj)
        {
            var t = new TransitionDef();
            if (obj["duration"] != null) t.duration = (float)obj["duration"];
            if (obj["delay"] != null) t.delay = (float)obj["delay"];
            if (obj["ease"] != null && System.Enum.TryParse<Ease>(obj["ease"].ToString(), true, out var e)) t.ease = e;
            
            
            return t;
        }

        private static RepeatDef ParseRepeat(JObject obj)
        {
            var r = new RepeatDef();
            if (obj["cycles"] != null)
            {
                r.cycles = (int)obj["cycles"];
            }

            if (obj["cycleMode"] != null && System.Enum.TryParse<CycleMode>(obj["cycleMode"].ToString(), true, out var cm))
            {
                r.cycleMode = cm;
            }

            return r;
        }
    }
}