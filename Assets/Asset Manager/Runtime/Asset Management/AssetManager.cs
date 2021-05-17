using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Asset_Manager.Runtime.Asset_Management;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Skywatch.AssetManagement
{
    /// <summary>
    /// Handles all loading, unloading, instantiating, and destroying of AssetReferences and their associated Objects.
    /// </summary>
    // TODO: Review pool system
    
    // TODO: Add cache for scenes
    // TODO: Refactor methods names in order to get more easy and consistent api. 
    // TODO: Take out all magic numbers (initial list capacity)
    public static class AssetManager
    {
        private const string BaseErr = "<color=#ffa500>" + nameof(AssetManager) + " Error:</color> ";

        public delegate void DelegateAssetLoaded(string key, AsyncOperationHandle handle);

        public static event DelegateAssetLoaded OnAssetLoaded;

        public delegate void DelegateAssetUnloaded(string runtimeKey);

        public static event DelegateAssetUnloaded OnAssetUnloaded;

        private static readonly Dictionary<string, AsyncOperationHandle> LoadingAssets =
            new Dictionary<string, AsyncOperationHandle>(20);

        private static readonly Dictionary<string, AsyncOperationHandle> LoadedAssets =
            new Dictionary<string, AsyncOperationHandle>(100);

        private static readonly Dictionary<string, List<GameObject>> InstantiatedObjects =
            new Dictionary<string, List<GameObject>>(10);

        // TODO: Remove this getter or keep only for debug purpose 
        public static IReadOnlyList<object> loadedAssets => LoadedAssets.Values.Select(x => x.Result).ToList();
        
        public static int loadedAssetsCount => LoadedAssets.Count;
        public static int loadingAssetsCount => LoadingAssets.Count;
        public static int instantiatedAssetsCount => InstantiatedObjects.Values.SelectMany(x => x).Count();

        #region Get

        // TODO: Rename to HasAsset
        public static bool IsLoaded(AssetReference aRef)
        {
            return LoadedAssets.ContainsKey((string) aRef.RuntimeKey);
        }

        public static bool IsLoaded(string key)
        {
            return LoadedAssets.ContainsKey(key);
        }

        public static bool IsLoading(AssetReference aRef)
        {
            return LoadingAssets.ContainsKey((string) aRef.RuntimeKey);
        }

        public static bool IsLoading(string key)
        {
            return LoadingAssets.ContainsKey(key);
        }

        // TODO: Rename to HasInstance
        public static bool IsInstantiated(AssetReference aRef)
        {
            return InstantiatedObjects.ContainsKey((string) aRef.RuntimeKey);
        }

        public static bool IsInstantiated(string key)
        {
            return InstantiatedObjects.ContainsKey(key);
        }

        public static int InstantiatedCount(AssetReference aRef)
        {
            return !IsInstantiated(aRef) ? 0 : InstantiatedObjects[(string) aRef.RuntimeKey].Count;
        }

        #endregion

        #region Load/Unload

        // TODO: add method LoadAllByLabel - scenes are also included
        // TODO: add method InstantiateAllByLabel - scenes are also included
        // TODO: add method TryGetOrLoadObjectAsync overload with Key

        /// <summary>
        /// DO NOT USE FOR <see cref="Component"/>s. Call <see cref="TryGetOrLoadComponentAsync{TComponentType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TComponentType})"/>
        ///
        /// Tries to get an already loaded <see cref="UnityEngine.Object"/> of type <see cref="T"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReference"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="T">The type of NON-COMPONENT object to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadObjectAsync<T>(AssetReference aRef, out AsyncOperationHandle<T> handle)
            where T : Object
        {
            _CheckRuntimeKey(aRef);
            return _TryGetOrLoadObjectAsyncInternal(aRef, null, out handle);
        }
        
        public static bool TryGetOrLoadObjectAsync<T>(string key, out AsyncOperationHandle<T> handle)
            where T : Object
        {
            return _TryGetOrLoadObjectAsyncInternal(null, key, out handle);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool _TryGetOrLoadObjectAsyncInternal<T>(AssetReference aRef, string key, out AsyncOperationHandle<T> handle) where T : Object
        {
            key = (string) aRef?.RuntimeKey ?? key;

            if (LoadedAssets.TryGetValue(key, out var loadedAsyncOp))
            {
                try
                {
                    handle = loadedAsyncOp.Convert<T>();
                }
                catch
                {
                    handle = Addressables.ResourceManager.CreateCompletedOperation(loadedAsyncOp.Result as T, string.Empty);
                }

                return true;
            }

            if (LoadingAssets.TryGetValue(key, out var loadingAsyncOp))
            {
                try
                {
                    handle = loadingAsyncOp.Convert<T>();
                }
                catch
                {
                    handle = _CreateChainOperation<T>(loadingAsyncOp);
                }

                return false;
            }

            handle = aRef == null ? Addressables.LoadAssetAsync<T>(key) : Addressables.LoadAssetAsync<T>(aRef);

            LoadingAssets.Add(key, handle);

            handle.Completed += op2 =>
            {
                LoadedAssets.Add(key, op2);
                LoadingAssets.Remove(key);

                OnAssetLoaded?.Invoke(key, op2);
            };

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AsyncOperationHandle<T> _CreateChainOperation<T>(AsyncOperationHandle asyncOp) where T : Object
        {
            return Addressables.ResourceManager.CreateChainOperation(asyncOp,
                chainOp => Addressables.ResourceManager.CreateCompletedOperation(chainOp.Result as T, string.Empty));
        }
        
        /// <summary>
        /// DO NOT USE FOR <see cref="Component"/>s. Call <see cref="TryGetOrLoadComponentAsync{TComponentType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TComponentType})"/>
        ///
        /// Tries to get an already loaded <see cref="UnityEngine.Object"/> of type <see cref="T"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReferenceT{TObject}"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="T">The type of NON-COMPONENT object to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        ///  TODO: Rmeove this method
        public static bool TryGetOrLoadObjectAsync<T>(AssetReferenceT<T> aRef, out AsyncOperationHandle<T> handle) where T : Object
        {
            return TryGetOrLoadObjectAsync(aRef as AssetReference, out handle);
        }

        /// <summary>
        /// DO NOT USE FOR <see cref="UnityEngine.Object"/>s. Call <see cref="TryGetOrLoadObjectAsync{TObjectType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TObjectType})"/>
        ///
        /// Tries to get an already loaded <see cref="Component"/> of type <see cref="TComponentType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReference"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TComponentType">The type of Component to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadComponentAsync<TComponentType>(AssetReference aRef,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            _CheckRuntimeKey(aRef);

            var key = (string) aRef.RuntimeKey;

            if (LoadedAssets.TryGetValue(key, out var loadedAsyncOp))
            {
                handle = _ConvertHandleToComponent<TComponentType>(loadedAsyncOp);
                return true;
            }

            if (LoadingAssets.TryGetValue(key, out var loadingAsyncOp))
            {
                handle = Addressables.ResourceManager.CreateChainOperation(loadingAsyncOp,
                    _ConvertHandleToComponent<TComponentType>);
                return false;
            }

            var op = Addressables.LoadAssetAsync<GameObject>(aRef);

            LoadingAssets.Add(key, op);

            op.Completed += op2 =>
            {
                LoadedAssets.Add(key, op2);
                LoadingAssets.Remove(key);

                OnAssetLoaded?.Invoke(key, op2);
            };

            handle = Addressables.ResourceManager.CreateChainOperation<TComponentType, GameObject>(op, chainOp =>
            {
                var go = chainOp.Result;
                var comp = go.GetComponent<TComponentType>();
                return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);
            });

            return false;
        }

        /// <summary>
        /// DO NOT USE FOR <see cref="UnityEngine.Object"/>s. Call <see cref="TryGetOrLoadObjectAsync{TObjectType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TObjectType})"/>
        ///
        /// Tries to get an already loaded <see cref="Component"/> of type <see cref="TComponentType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReferenceT{TObject}"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TComponentType">The type of Component to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadComponentAsync<TComponentType>(AssetReferenceT<TComponentType> aRef,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            return TryGetOrLoadComponentAsync(aRef as AssetReference, out handle);
        }

        #region Sync methods

        public static bool TryGetObjectSync<TObjectType>(AssetReference aRef, out TObjectType result)
            where TObjectType : Object
        {
            _CheckRuntimeKey(aRef);
            var key = (string) aRef.RuntimeKey;

            if (LoadedAssets.ContainsKey(key))
            {
                result = LoadedAssets[key].Convert<TObjectType>().Result;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetObjectSync<TObjectType>(AssetReferenceT<TObjectType> aRef, out TObjectType result)
            where TObjectType : Object
        {
            return TryGetObjectSync(aRef as AssetReference, out result);
        }

        public static bool TryGetComponentSync<TComponentType>(AssetReference aRef, out TComponentType result)
            where TComponentType : Component
        {
            _CheckRuntimeKey(aRef);
            var key = (string) aRef.RuntimeKey;

            // TODO: Replace with TryGet
            if (LoadedAssets.ContainsKey(key))
            {
                var handle = LoadedAssets[key];
                result = null;
                var go = handle.Result as GameObject;
                if (!go)
                    throw new ConversionException($"Cannot convert {nameof(handle.Result)} to {nameof(GameObject)}.");
                result = go.GetComponent<TComponentType>();
                if (!result)
                    throw new ConversionException(
                        $"Cannot {nameof(go.GetComponent)} of Type {typeof(TComponentType)}.");
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetComponentSync<TComponentType>(AssetReferenceT<TComponentType> aRef,
            out TComponentType result) where TComponentType : Component
        {
            return TryGetComponentSync(aRef as AssetReference, out result);
        }

        #endregion

        public static AsyncOperationHandle<List<AsyncOperationHandle<Object>>> LoadAssetsByLabelAsync(string label)
        {
            var loadByLabelOperation =
                new LoadAssetsByLabelOperation(label, LoadedAssets, LoadingAssets, _OnAssetLoadedCallback);
            return Addressables.ResourceManager.StartOperation(loadByLabelOperation, default);
        }

        private static void _OnAssetLoadedCallback(string key, AsyncOperationHandle handle)
        {
            OnAssetLoaded?.Invoke(key, handle);
        }

        /// <summary>
        /// Unloads the given <paramref name="aRef"/> and calls <see cref="DestroyAllInstances"/> if it was Instantiated.
        /// </summary>
        /// <param name="aRef"></param>
        /// <returns></returns>
        public static void Unload(AssetReference aRef)
        {
            _CheckRuntimeKey(aRef);

            Unload(aRef.RuntimeKey.ToString());
        }

        public static void Unload(string key)
        {
            if (LoadedAssets.TryGetValue(key, out var handle))
                LoadedAssets.Remove(key);
            else if (LoadingAssets.TryGetValue(key, out handle))
                LoadingAssets.Remove(key);
            else
            {
                Debug.LogWarning($"{BaseErr}Cannot {nameof(Unload)} RuntimeKey '{key}': It is not loading or loaded.");
                return;
            }

            if (IsInstantiated(key))
                DestroyAllInstances(key);

            Addressables.Release(handle);

            OnAssetUnloaded?.Invoke(key);
        }

        /// <summary>
        /// Unload all assets by given label
        /// </summary>
        /// <param name="label"></param>
        public static void UnloadByLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrWhiteSpace(label))
            {
                Debug.LogError("Label cannot be empty.");
                return;
            }

            var locationsHandle = Addressables.LoadResourceLocationsAsync(label);
            locationsHandle.Completed += op =>
            {
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Cannot Unload by label '{label}'");
                    return;
                }

                var keys = _GetKeysFromLocations(op.Result);
                foreach (var key in keys)
                {
                    Unload(key);
                }
            };
        }

        #endregion

        #region Instantiation

        public static bool TryInstantiateOrLoadAsync(AssetReference aRef, Vector3 position, Quaternion rotation,
            Transform parent,
            out AsyncOperationHandle<GameObject> handle)
        {
            if (TryGetOrLoadObjectAsync(aRef, out AsyncOperationHandle<GameObject> loadHandle))
            {
                var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                handle = Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<GameObject>(null,
                    $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                return Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
            });
            return false;
        }

        //Returns an AsyncOperationHandle<TComponentType> with the result set to an instantiated Component.
        public static bool TryInstantiateOrLoadAsync<TComponentType>(AssetReference aRef, Vector3 position,
            Quaternion rotation, Transform parent,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            if (TryGetOrLoadComponentAsync(aRef, out AsyncOperationHandle<TComponentType> loadHandle))
            {
                var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                handle = Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<TComponentType>(null,
                    $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            //Create a chain that waits for loadHandle to finish, then instantiates and returns the instance GO.
            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                return Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
            });
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aRef"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="parent"></param>
        /// <param name="handle"></param>
        /// <typeparam name="TComponentType"></typeparam>
        /// <returns></returns>
        public static bool TryInstantiateOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef,
            Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            return TryInstantiateOrLoadAsync(aRef as AssetReference, position, rotation, parent, out handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aRef"></param>
        /// <param name="count"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="parent"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool TryInstantiateMultiOrLoadAsync(AssetReference aRef, int count, Vector3 position,
            Quaternion rotation, Transform parent,
            out AsyncOperationHandle<List<GameObject>> handle)
        {
            if (TryGetOrLoadObjectAsync(aRef, out AsyncOperationHandle<GameObject> loadHandle))
            {
                var list = new List<GameObject>(count);
                for (var i = 0; i < count; i++)
                {
                    var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                    list.Add(instance);
                }

                handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<List<GameObject>>(null,
                    $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var list = new List<GameObject>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                    list.Add(instance);
                }

                return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
            });
            return false;
        }

        public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReference aRef, int count,
            Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        {
            if (TryGetOrLoadComponentAsync(aRef, out AsyncOperationHandle<TComponentType> loadHandle))
            {
                var list = new List<TComponentType>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                    list.Add(instance);
                }

                handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<List<TComponentType>>(null,
                    $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var list = new List<TComponentType>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                    list.Add(instance);
                }

                return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
            });

            return false;
        }

        public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef,
            int count, Vector3 position, Quaternion rotation,
            Transform parent, out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        {
            return TryInstantiateMultiOrLoadAsync(aRef as AssetReference, count, position, rotation, parent,
                out handle);
        }

        public static bool TryInstantiateSync(AssetReference aRef, Vector3 position, Quaternion rotation,
            Transform parent, out GameObject result)
        {
            if (!TryGetObjectSync(aRef, out GameObject loadResult))
            {
                result = null;
                return false;
            }

            result = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
            return true;
        }

        public static bool TryInstantiateSync<TComponentType>(AssetReference aRef, Vector3 position,
            Quaternion rotation, Transform parent,
            out TComponentType result) where TComponentType : Component
        {
            if (!TryGetComponentSync(aRef, out TComponentType loadResult))
            {
                result = null;
                return false;
            }

            result = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
            return true;
        }

        public static bool TryInstantiateSync<TComponentType>(AssetReferenceT<TComponentType> aRef, Vector3 position,
            Quaternion rotation, Transform parent,
            out TComponentType result) where TComponentType : Component
        {
            return TryInstantiateSync(aRef as AssetReference, position, rotation, parent, out result);
        }

        public static bool TryInstantiateMultiSync(AssetReference aRef, int count, Vector3 position,
            Quaternion rotation, Transform parent,
            out List<GameObject> result)
        {
            if (!TryGetObjectSync(aRef, out GameObject loadResult))
            {
                result = null;
                return false;
            }

            var list = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var instance = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
                list.Add(instance);
            }

            result = list;
            return true;
        }

        public static bool TryInstantiateMultiSync<TComponentType>(AssetReferenceT<TComponentType> aRef, int count,
            Vector3 position, Quaternion rotation, Transform parent,
            out List<TComponentType> result) where TComponentType : Component
        {
            return TryInstantiateMultiSync(aRef as AssetReference, count, position, rotation, parent, out result);
        }

        public static bool TryInstantiateMultiSync<TComponentType>(AssetReference aRef, int count, Vector3 position,
            Quaternion rotation, Transform parent,
            out List<TComponentType> result) where TComponentType : Component
        {
            if (!TryGetComponentSync(aRef, out TComponentType loadResult))
            {
                result = null;
                return false;
            }

            var list = new List<TComponentType>(count);
            for (int i = 0; i < count; i++)
            {
                var instance = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
                list.Add(instance);
            }

            result = list;
            return true;
        }

        private static TComponentType _InstantiateInternal<TComponentType>(AssetReference aRef, TComponentType loadedAsset,
            Vector3 position, Quaternion rotation, Transform parent)
            where TComponentType : Component
        {
            var key = (string) aRef.RuntimeKey;

            var instance = Object.Instantiate(loadedAsset, position, rotation, parent);
            if (!instance)
                throw new NullReferenceException($"Instantiated Object of type '{typeof(TComponentType)}' is null.");

            var monoTracker = instance.gameObject.AddComponent<MonoTracker>();
            monoTracker.key = key;
            monoTracker.OnDestroyed += _OnTrackerDestroyed;

            if (!InstantiatedObjects.ContainsKey(key))
                InstantiatedObjects.Add(key, new List<GameObject>(20));

            InstantiatedObjects[key].Add(instance.gameObject);

            return instance;
        }

        //
        private static GameObject _InstantiateInternal(AssetReference aRef, GameObject loadedAsset, Vector3 position,
            Quaternion rotation, Transform parent)
        {
            var key = (string) aRef.RuntimeKey;

            var instance = Object.Instantiate(loadedAsset, position, rotation, parent);
            if (!instance)
                throw new NullReferenceException($"Instantiated Object of type '{typeof(GameObject)}' is null.");

            var monoTracker = instance.gameObject.AddComponent<MonoTracker>();
            monoTracker.key = key;
            monoTracker.OnDestroyed += _OnTrackerDestroyed;

            if (!InstantiatedObjects.ContainsKey(key))
                InstantiatedObjects.Add(key, new List<GameObject>(20));
            
            InstantiatedObjects[key].Add(instance);
            return instance;
        }

        private static void _OnTrackerDestroyed(MonoTracker tracker)
        {
            if (InstantiatedObjects.TryGetValue(tracker.key, out var list))
                list.Remove(tracker.gameObject);
        }

        /// <summary>
        /// Destroys all instantiated instances of <paramref name="aRef"/>
        /// </summary>
        public static void DestroyAllInstances(AssetReference aRef)
        {
            var key = (string) aRef.RuntimeKey;
           
            if (!InstantiatedObjects.ContainsKey(key))
            {
                Debug.LogWarning(
                    $"{nameof(AssetReference)} '{aRef}' has not been instantiated. 0 Instances destroyed.");
                return;
            }

            DestroyAllInstances(key);
        }

        public static void DestroyAllInstances(string key)
        {
            var instanceList = InstantiatedObjects[key];
            for (var i = instanceList.Count - 1; i >= 0; i--)
                _DestroyInternal(instanceList[i]);

            InstantiatedObjects[key].Clear();
            InstantiatedObjects.Remove(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _DestroyInternal(Object obj)
        {
            var c = obj as Component;
            Object.Destroy(c ? c.gameObject : obj);
        }

        #endregion

        #region Utilities

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _CheckRuntimeKey(AssetReference aRef)
        {
#if DEBUG
            if (!aRef.RuntimeKeyIsValid())
                throw new InvalidKeyException($"{BaseErr}{nameof(aRef.RuntimeKey)} is not valid for '{aRef}'.");
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<string> _GetKeysFromLocations(IList<IResourceLocation> locations)
        {
            var keys = new List<string>(locations.Count);

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var keyObj in locator.Keys)
                {
                    var key = keyObj.ToString();
                    
                    if (!Guid.TryParse(key, out _) || !_TryGetKeyLocationID(locator, key, out var keyLocationID))
                        continue;

                    // TODO: Optimize this linq
                    var locationMatched = locations.Select(x => x.InternalId).ToList().Exists(x => x == keyLocationID);
                    if (!locationMatched)
                        continue;

                    keys.Add(key);
                }
            }

            return keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool _TryGetKeyLocationID(IResourceLocator locator, string key, out string internalID)
        {
            internalID = string.Empty;

            var hasLocation = locator.Locate(key, typeof(Object), out var keyLocations);

            if (!hasLocation || keyLocations.Count != 1)
                return false;

            internalID = keyLocations[0].InternalId;
            return true;
        }

        #endregion
    }
}