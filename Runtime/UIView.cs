using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UIFramework
{
    public abstract class UIView : UIBase
    {
        public abstract ViewType Type { get; } 

        protected override void ConfigureCanvas()
        {
            // Views MUST override sorting to float above the world/other views
            if (Canvas != null)
            {
                Canvas.overrideSorting = true;
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