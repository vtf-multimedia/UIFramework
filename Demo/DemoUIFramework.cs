using System;
using Cysharp.Threading.Tasks;
using UISystem;
using UISystem.Demo;
using UnityEngine;

namespace UIFramework.Demo
{
    public class DemoUIFramework : MonoBehaviour
    {
        private async void Start()
        {
            var demoView = await UIManager.Instance.ShowViewAsync<UIDemoView>();
            
        }
        
        
        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                UIManager.Instance.HideViewAsync<UIDemoView>();
            }
        }
    }
}