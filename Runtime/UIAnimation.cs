using System;
using UnityEngine;
using PrimeTween;
using Cysharp.Threading.Tasks;

namespace UIFramework
{
    [RequireComponent(typeof(UIStyle))]
    public class UIAnimation : MonoBehaviour
    {
        private UIStyle _style;
        private AnimationDef _config;
        
        // The "Anchor" style (Inspector + Base JSON)
        private StyleState _normalState => _style ? _style.NormalState : StyleState.Default;

        // Tracks the background timeline (Show/Loop)
        private Tween _timelineTween;
        // Tracks the foreground interaction (Hover/Press)
        private Tween _interactionTween;
        
        // State Tracking
        private bool _isLoopingActive; // Are we supposed to be looping right now?
        private AnimationState _currentInteraction = AnimationState.Normal;

        private void Awake() => _style = GetComponent<UIStyle>();

        public void Setup(AnimationDef config)
        {
            _config = config;
        }

        public void Stop()
        {
            if (_timelineTween.isAlive) _timelineTween.Stop();
            if (_interactionTween.isAlive) _interactionTween.Stop();
            _isLoopingActive = false;
            _currentInteraction = AnimationState.Normal;
        }

        // ===================================================================================
        // 1. PUBLIC API (Lifecycle)
        // ===================================================================================

        public async UniTask PlayShow()
        {
            if (_config == null) return;
            Stop();

            if (_config.Enter != null)
            {
                var enterState = StyleState.Merge(_style.NormalState, _config.Enter.Style); 
                _style.Apply(enterState);

                if (_config.Repeat != null && _config.Repeat.cycles != 0 && _config.Initial != null && _config.Animate != null)
                {
                    var initialState = ResolveState(AnimationState.Initial);
                    var transition = ResolveTransition(AnimationState.Enter);
                    await RunTimelineTween(initialState, transition);
                    StartAnimateTimeline().Forget();
                }
                else
                {
                    var normalState = ResolveState(AnimationState.Normal);
                    var transition = ResolveTransition(AnimationState.Enter);
                    await RunTimelineTween(normalState, transition);
                }
            }
            else
            {
                if (_config.Repeat != null && _config.Repeat.cycles != 0 && _config.Initial != null && _config.Animate != null)
                {
                    var initialState = ResolveState(AnimationState.Initial);
                    _style.Apply(initialState);
                    StartAnimateTimeline().Forget();
                }
                else
                {
                    var normalState = ResolveState(AnimationState.Normal);
                    _style.Apply(normalState);
                }
            }
        }

        // private void Update()
        // {
        //     if (_timelineTween.isAlive)
        //     {
        //         Debug.Log($"{_timelineTween.isAlive}, {_timelineTween.progressTotal}, {_timelineTween.elapsedTime}, {_timelineTween.elapsedTimeTotal}");
        //     }
        //     else
        //     {
        //         Debug.Log($"{_timelineTween.isAlive}");
        //     }
        // }

        public async UniTask PlayHide()
        {
            if (_config?.Exit != null)
            {
                // Hard Stop everything
                Stop(); 
                
                var exitState = StyleState.Merge(_normalState, _config.Exit.Style);
                // Play Exit tween
                var transition = ResolveTransition(AnimationState.Exit);
                await RunTimelineTween(exitState, transition);
            }
        }

        public void PlayState(string key)
        {
            if (_config == null) return;
            if (_timelineTween.isAlive) return;
            // Map string to Enum
            (bool success, AnimationState requestedState) = ParseState(key);
            if (!success || !HasState(requestedState)) return;

            // Filter: Ignore repeated calls
            if (_currentInteraction == requestedState) return;
            var transition = ResolveTransition(requestedState);
            if (requestedState == AnimationState.Normal)
            {
               transition = ResolveTransition(_currentInteraction); 
            }
            _currentInteraction = requestedState;
            
            StyleState target = ResolveState(_currentInteraction);
            RunInteractionTween(target, transition);
        }



        private async UniTaskVoid StartAnimateTimeline()
        {
            var isInfinite = _config.Repeat.cycles == -1;
            var repeatCount = _config.Repeat.cycles == 0 ? 1 : _config.Repeat.cycles;
            var cycleMode = _config.Repeat.cycleMode;

            var initialState = ResolveState(AnimationState.Initial);
            var animateState = ResolveState(AnimationState.Animate);

            var duration = _config.Transition.duration;
            var ease = _config.Transition.ease;
            var delay = _config.Transition.delay;

            _isLoopingActive = true;
            _timelineTween = Tween.Custom(
                0, 1, duration,
                ease: ease,
                onValueChange: t =>
                {
                    _style.Apply(StyleState.Lerp(initialState, animateState, t));
                }, 
                cycleMode: cycleMode,
                cycles: repeatCount
            );
            await _timelineTween.ToUniTask();

            _isLoopingActive = false;

            var normalState = ResolveState(AnimationState.Normal);
            var currentState = _style.CurrentState;
            _timelineTween = Tween.Custom(
                0, 1, duration,
                ease: ease,
                onValueChange: t =>
                {
                    _style.Apply(StyleState.Lerp(currentState, normalState, t));
                }
            );
        }
        // ===================================================================================
        // HELPERS
        // ===================================================================================
        
        private async UniTask RunTimelineTween(StyleState target, TransitionDef settings)
        {
            if (_timelineTween.isAlive) _timelineTween.Stop();

            StyleState start = _style.CurrentState;
            
            _timelineTween = Tween.Custom(0, 1, settings.duration, ease: settings.ease, 
                onValueChange: t => _style.Apply(StyleState.Lerp(start, target, t)));

            await _timelineTween.ToUniTask();
        }

        private async UniTask RunInteractionTween(StyleState target, TransitionDef settings)
        {
            if (_interactionTween.isAlive) _interactionTween.Stop();

            var start = _style.CurrentState;


            _interactionTween = Tween.Custom(0, 1, duration: settings.duration, ease: settings.ease,
                onValueChange: t =>
                {
                    _style.Apply(StyleState.Lerp(start, target, t));
                });

            await _interactionTween.ToUniTask();
        }

        private bool HasState(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Normal:  return true;
                case AnimationState.Enter:   return (_config.Enter != null);
                case AnimationState.Exit:    return (_config.Exit != null);
                case AnimationState.Initial: return (_config.Initial != null);
                case AnimationState.Animate: return (_config.Animate != null);
                case AnimationState.Hover:   return (_config.Hover != null);
                case AnimationState.Press:   return (_config.Press != null);
                case AnimationState.Check:   return (_config.Check != null);
                default: return false;
            }
        }

        private StyleState ResolveState(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Normal:  return _normalState;
                case AnimationState.Enter:   return (_config.Enter != null) ? StyleState.Merge(_normalState, _config.Enter.Style) : _normalState;
                case AnimationState.Exit:    return (_config.Exit != null) ? StyleState.Merge(_normalState, _config.Exit.Style) : _normalState;
                case AnimationState.Initial: return (_config.Initial != null) ? StyleState.Merge(_normalState, _config.Initial.Style) : _normalState;
                case AnimationState.Animate: return (_config.Animate != null) ? StyleState.Merge(_normalState, _config.Animate.Style) : _normalState;
                case AnimationState.Hover:   return (_config.Hover != null) ? StyleState.Merge(_normalState, _config.Hover.Style) : _normalState;
                case AnimationState.Press:   return (_config.Press != null) ? StyleState.Merge(_normalState, _config.Press.Style) : _normalState;
                case AnimationState.Check:   return (_config.Check != null) ? StyleState.Merge(_normalState, _config.Check.Style) : _normalState;
                default: return _normalState;
            }
        }

        private TransitionDef ResolveTransition(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Normal: return _config.Transition;
                case AnimationState.Enter: return _config.Enter.Transition ?? _config.Transition;
                case AnimationState.Exit: return _config.Exit.Transition ?? _config.Transition;
                case AnimationState.Initial: return _config.Initial.Transition ?? _config.Transition;
                case AnimationState.Animate: return _config.Animate.Transition ?? _config.Transition;
                case AnimationState.Check: return _config.Check.Transition ?? _config.Transition;
                case AnimationState.Press: return _config.Press.Transition ?? _config.Transition;
                case AnimationState.Hover: return _config.Hover.Transition ?? _config.Transition;
                default: return _config.Transition;
            }
        }

        private (bool, AnimationState) ParseState(string key)
        {
            switch (key.ToLower())
            {
                case "hover": return (true, AnimationState.Hover);
                case "press": return (true, AnimationState.Press);
                case "check": return (true, AnimationState.Check);
                case "normal": return (true, AnimationState.Normal);
                default: return (false, AnimationState.Normal);
            }
        }
    }
}