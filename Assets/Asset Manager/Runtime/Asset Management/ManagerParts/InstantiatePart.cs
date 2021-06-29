using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Asset_Manager.Runtime.Asset_Management;
using Asset_Manager.Runtime.Asset_Management.Pooling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets
{
    public static partial class AssetManager
    {
        public static bool InstantiatePrefab(string key, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(null, key, out handle);
        }
        
        public static bool InstantiatePrefab(string key, Vector3 position, Quaternion rotation, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(null, key, out handle, position, rotation);
        }
        
        public static bool InstantiatePrefab(string key, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(null, key, out handle, position, rotation, parent);
        }
        
        public static bool InstantiatePrefab(AssetReference aRef, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(aRef, null, out handle);
        }
        
        public static bool InstantiatePrefab(AssetReference aRef, Vector3 position, Quaternion rotation, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(aRef, null, out handle, position, rotation);
        }
        
        public static bool InstantiatePrefab(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<GameObject> handle)
        {
            return _InstantiatePrefab(aRef, null, out handle, position, rotation, parent);
        }
        
        /// <summary>
        /// Returns 'true' if prefab instantly has been instantiated, 'false' if it is needed time, so async instantiation. 
        /// </summary>
        private static bool _InstantiatePrefab(AssetReference aRef, string key, out AsyncOperationHandle<GameObject> handle, 
            Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            AsyncOperationHandle<GameObject> loadHandle;
            var isRef = aRef != null;
            key = isRef ? aRef.RuntimeKey.ToString() : key;
            var isLoaded = isRef ? LoadAsset(aRef, out loadHandle) : LoadAsset(key, out loadHandle);
            var instantiationParams = new InstantiationParameters(position, rotation, parent);
            
            if (isLoaded)
            {
                handle = _CreateCompletedPrefabOperation(_InstantiateGeneric(key, loadHandle.Result, ref instantiationParams));
                return true;
            }
            
            // TODO: This is exceptional case. An exception should be thrown.
            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<GameObject>(null,
                    $"Load Operation was invalid: {loadHandle}.");
                return false;
            }
            
            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp => 
                _CreateCompletedPrefabOperation(_InstantiateGeneric(key, loadHandle.Result, ref instantiationParams)));
            
            return false;
        }

        //
        public static bool InstantiateComponent<TComponentType>(AssetReferenceT<TComponentType> aRef, out AsyncOperationHandle<TComponentType> handle, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where TComponentType : Component
        {
            return _InstantiateComponent(aRef, null, out handle, position, rotation, parent);
        }

        public static bool InstantiateComponent<TComponentType>(string key, out AsyncOperationHandle<TComponentType> handle, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where TComponentType : Component
        {
            return _InstantiateComponent(null, key, out handle, position, rotation, parent);
        }

        private static bool _InstantiateComponent<TComponent>(AssetReference aRef, string key, out AsyncOperationHandle<TComponent> handle,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where TComponent : Component
        {
            AsyncOperationHandle<TComponent> loadHandle;
            var isRef = aRef != null;
            key = isRef ? aRef.RuntimeKey.ToString() : key;
            var isLoaded = isRef ? LoadAsset(aRef, out loadHandle) : LoadAsset(key, out loadHandle);
            var instantiationParams = new InstantiationParameters(position, rotation, parent);
            
            if (isLoaded)
            {
                var instance = _InstantiateGeneric(key, loadHandle.Result, ref instantiationParams);
                handle = Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
            }
            else
            {
                handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp => 
                    _ConvertHandleToComponent<TComponent>(chainOp));
            }

            return isLoaded;
        }
                
        private static AsyncOperationHandle<TComponentType> _ConvertHandleToComponent<TComponentType>(AsyncOperationHandle handle) where TComponentType : Component
        {
            var go = handle.Result as GameObject;

            if (!go)
                throw new ConversionException($"Cannot convert {nameof(handle.Result)} to {nameof(GameObject)}.");

            if (go.TryGetComponent<TComponentType>(out var comp))
                return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);

            throw new ConversionException($"Cannot {nameof(go.GetComponent)} of Type {typeof(TComponentType)}.");
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T _InstantiateGeneric<T>(string key, T loadedAsset, ref InstantiationParameters instParams) where T: Object
        {
            var instance = instParams.Instantiate(loadedAsset);
            
            if (!instance)
                throw new NullReferenceException($"Instantiated Object of type '{typeof(GameObject)}' is null.");

            if (!InstantiatedObjects.ContainsKey(key))
                InstantiatedObjects.Add(key, new List<Object>(20));

            InstantiatedObjects[key].Add(instance);
            
            instance.AddMonoTracker(key, _OnTrackerDestroyed);
            
            return instance;
        }

        private static void _OnTrackerDestroyed(MonoTracker tracker)
        {
            if (InstantiatedObjects.TryGetValue(tracker.key, out var list))
                list.Remove(tracker.gameObject);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AsyncOperationHandle<GameObject> _CreateCompletedPrefabOperation(GameObject gameObject)
        {
            return Addressables.ResourceManager.CreateCompletedOperation(gameObject, string.Empty);
        }
    }
}