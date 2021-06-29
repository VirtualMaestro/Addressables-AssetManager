using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asset_Manager.Runtime.Asset_Management.Pooling
{
    public static class ExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if(source == null)
                Debug.LogException(new NullReferenceException());
            if(action == null)
                Debug.LogException(new NullReferenceException());

            foreach(var element in source)
            {
                action(element);
            }
        }
        
        public static void AddMonoTracker(this Object uObject, string key, MonoTracker.DelegateDestroyed monoDestroyCallback)
        {
            var gameObject = uObject as GameObject;
            gameObject = gameObject == null ? ((Component) uObject).gameObject: gameObject;
            
            var monoTracker = gameObject.AddComponent<MonoTracker>();
            monoTracker.key = key;
            monoTracker.OnDestroyed += monoDestroyCallback;
        }
    }
}
