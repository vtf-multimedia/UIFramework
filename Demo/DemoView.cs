using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UIFramework.Demo
{
    public class DemoView : UIView
    {
        public override ViewType Type => ViewType.Screen;
        
        [SerializeField] private TMP_Text _text;
        [SerializeField] private List<DemoComponent> _demoComponents = new();


        public void SetText(string text)
        {
            _text.text = text;
        }

        protected override async UniTask OnShow()
        {
            foreach (var demoComponent in _demoComponents)
            {
                await demoComponent.Show();
            }
        }

        protected override async UniTask OnHide()
        {
            foreach (var demoComponent in _demoComponents)
            {
                await demoComponent.Hide();
            }
        }
    }
}