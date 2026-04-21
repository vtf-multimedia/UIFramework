using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace UIFramework
{
    public class UIComponent : UIBase
    {
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
    }
}