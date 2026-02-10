using UnityEngine;
using System.Collections.Generic;

namespace UIFramework
{
    [RequireComponent(typeof(UIBase))]
    public class UIIdentity : MonoBehaviour
    {
        [Tooltip("Unique ID for singleton lookup (e.g. 'InventoryWindow')")]
        public string ID;

        [Tooltip("Shared style classes (e.g. 'window', 'dark_theme')")]
        public List<string> Classes = new List<string>();
    }
}