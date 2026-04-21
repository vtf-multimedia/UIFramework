using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using PrimeTween;
using System;

namespace UISystem
{
    /// <summary>
    /// Core animation utility using PrimeTween to interpolate between StyleStates.
    /// </summary>
    public static class UIAnimation
    {
        public static async UniTask AnimateAsync(VisualElement element, StyleState from, StyleState to, TransitionDef transition)
        {
            if (element == null) return;

            // Mapping JSON string ease to PrimeTween Ease enum
            Ease ease = FadeToPrimeTweenEase(transition.ease);

            await Tween.Custom(0f, 1f, duration: transition.duration, ease: ease, onValueChange: (t) =>
            {
                if (element == null) return;
                
                // Interpolate state
                StyleState interpolated = StyleState.Lerp(from, to, t);
                
                // Apply to element
                UIStyleBridge.Apply(element, interpolated);
            });
        }

        private static Ease FadeToPrimeTweenEase(string easeName)
        {
            if (string.IsNullOrEmpty(easeName)) return Ease.OutQuad;
            
            if (Enum.TryParse<Ease>(easeName, true, out var result))
            {
                return result;
            }

            // Fallback common alternates
            return easeName.ToLower() switch
            {
                "linear" => Ease.Linear,
                "easyin" => Ease.InQuad,
                "easyout" => Ease.OutQuad,
                "easyinout" => Ease.InOutQuad,
                _ => Ease.OutQuad
            };
        }
    }
}