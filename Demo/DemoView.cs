using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UIFramework.Demo
{
    public class DemoView : UIView
    {
        public override ViewType Type => ViewType.Screen;
        [SerializeField] private DemoComponent _demoComponentPrefab;
        [SerializeField] private TMP_Text _text;
        private DemoComponent _demoComponent;


        protected override void Awake()
        {
            base.Awake();
            _demoComponent = Instantiate(_demoComponentPrefab, transform);
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        protected override async UniTask OnShow()
        {
            await base.OnShow();
            await _demoComponent.Show();
            // foreach (var demoComponent in _demoComponents)
            // {
            //     await demoComponent.Show();
            // }
        }

        protected override async UniTask OnHide()
        {
            await _demoComponent.Hide();
            await base.OnHide();
            // foreach (var demoComponent in _demoComponents)
            // {
            //     await demoComponent.Hide();
            // }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                _demoComponent.Identity.SetID("demoComponent-selected");   
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                _demoComponent.Identity.SetID("demoComponent");
            }
        }
    }
}