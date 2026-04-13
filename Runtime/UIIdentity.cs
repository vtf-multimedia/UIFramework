using System;
using UnityEngine;
using System.Collections.Generic;

namespace UIFramework
{
    public class UIIdentity : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("ID")]
        [SerializeField] private string _id;

        [UnityEngine.Serialization.FormerlySerializedAs("Classes")]
        [SerializeField] private List<string> _classes = new();

        public string ID
        {
            set
            {
                _id = value;
                OnUpdateIdentity?.Invoke();
            }
            get
            {
                return _id;
            }
        }

        public List<string> Classes { get; }

        public Action OnUpdateIdentity;
        
        public void AddClass(string className)
        {
            _classes.Add(className);
            OnUpdateIdentity?.Invoke();
        }
        
        public void RemoveClass(string className)
        {
            _classes.Remove(className);
            OnUpdateIdentity?.Invoke();
        }

        public void ClearClasses()
        {
            _classes.Clear();
            OnUpdateIdentity?.Invoke();
        }
    }
}