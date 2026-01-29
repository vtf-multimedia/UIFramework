using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFramework.Demo
{
    public class DemoUIFramework : MonoBehaviour
    {
        private async void Start()
        {
            var demoView =  UIManager.Instance.GetView<DemoView>("DemoView");
            await UniTask.WaitForSeconds(1);
            demoView.SetText("Hello world");
            await UIManager.Instance.Show(demoView);
        }


        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                UIManager.Instance.Hide("DemoView").Forget();
            }
        }
    }
}