using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace UIFramework
{
    public class UIComponent : UIBase, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Toggle _toggle;

        protected override void Awake()
        {
            base.Awake();
            _toggle = GetComponent<Toggle>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(ToggleValueChanged);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveAllListeners();
            }
        }

        protected override void ConfigureCanvas()
        {
            if (Canvas != null)
            {
                Canvas.overrideSorting = false;
            }
        }

        protected override async UniTask OnShow()
        {
            await UniTask.CompletedTask;
        }

        protected override async UniTask OnHide()
        {
            await UniTask.CompletedTask;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // IsHovered = true;
            SetState("hover", true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // IsHovered = false;
            SetState("hover", false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetState("press", true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetState("press", false);
        }

        private void ToggleValueChanged(bool isOn)
        {
            SetState("check", isOn);
        }
    }
}