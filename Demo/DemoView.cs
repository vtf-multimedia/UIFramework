using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UISystem;
using UnityEngine;

namespace UIFramework.Demo
{
    public class DemoView : UIView
    {
        protected override string UxmlPath { get; }
        protected override void QueryElements()
        {
            
        }

        protected override void BindEvents()
        {
        }

        protected override void UnbindEvents()
        {
        }

        public override UILayer Layer { get; }
    }
}