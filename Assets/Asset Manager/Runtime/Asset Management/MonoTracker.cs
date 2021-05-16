using UnityEngine;

namespace Asset_Manager.Runtime.Asset_Management
{
    public class MonoTracker : MonoBehaviour
    {
        public delegate void DelegateDestroyed(MonoTracker tracker);

        public event DelegateDestroyed OnDestroyed;

        // TODO: Key as a string
        public object key { get; set; }

        void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
    }
}