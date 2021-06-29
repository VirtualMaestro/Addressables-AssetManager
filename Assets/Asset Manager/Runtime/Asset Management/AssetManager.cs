using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Handles all loading, unloading, instantiating, and destroying of AssetReferences and their associated Objects.
    /// </summary>
    // TODO: Review pool system
    // TODO: Add cache for scenes
    // TODO: Refactor methods names in order to get more easy and consistent api. 
    // TODO: Take out all magic numbers (initial list capacity)
    public static partial class AssetManager
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

        private static readonly Dictionary<string, List<Object>> InstantiatedObjects =
            new Dictionary<string, List<Object>>(10);

        // TODO: Remove this getter or keep only for debug purpose 
        public static IReadOnlyList<object> loadedAssets => LoadedAssets.Values.Select(x => x.Result).ToList();
        
        // public static int loadedAssetsCount => LoadedAssets.Count;
        // public static int loadingAssetsCount => LoadingAssets.Count;
        // public static int instantiatedAssetsCount => InstantiatedObjects.Values.SelectMany(x => x).Count();

        public static bool HasLoadedAsset(AssetReference aRef) => LoadedAssets.ContainsKey((string) aRef.RuntimeKey);
        public static bool HasLoadedAsset(string key) => LoadedAssets.ContainsKey(key);

        public static bool IsLoading(AssetReference aRef) => LoadingAssets.ContainsKey((string) aRef.RuntimeKey);
        public static bool IsLoading(string key) => LoadingAssets.ContainsKey(key);

        public static bool HasInstance(AssetReference aRef) => InstantiatedObjects.ContainsKey((string) aRef.RuntimeKey); 
        public static bool HasInstance(string key) => InstantiatedObjects.ContainsKey(key);

        public static int NumInstances(AssetReference aRef) => !HasInstance(aRef) ? 0 : InstantiatedObjects[(string) aRef.RuntimeKey].Count;

        // TODO: add method LoadAllByLabel - scenes are also included
        // TODO: add method InstantiateAllByLabel - scenes are also included
        // TODO: add method TryGetOrLoadObjectAsync overload with Key

        /// <summary>
        /// Unloads asset and removes all the created instances from this asset.
        /// </summary>
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

            if (HasInstance(key))
                DestroyAllInstances(key);

            Addressables.Release(handle);

            OnAssetUnloaded?.Invoke(key);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _CheckRuntimeKey(AssetReference aRef)
        {
#if DEBUG
            if (!aRef.RuntimeKeyIsValid())
                throw new InvalidKeyException($"{BaseErr}{nameof(aRef.RuntimeKey)} is not valid for '{aRef}'.");
#endif
        }
    }
}