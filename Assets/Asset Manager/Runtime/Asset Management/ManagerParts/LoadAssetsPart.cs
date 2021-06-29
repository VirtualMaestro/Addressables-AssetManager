using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.AddressableAssets
{
    public static partial class AssetManager
    {
        public static bool LoadAsset<T>(AssetReference aRef, out AsyncOperationHandle<T> handler) where T: Object
        {
            _CheckRuntimeKey(aRef);

            return _LoadAssetInternal(aRef, null, out handler);
        }

        public static bool LoadAsset<T>(string key, out AsyncOperationHandle<T> handler) where T: Object
        {
            return _LoadAssetInternal(null, key, out handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool _LoadAssetInternal<T>(AssetReference aRef, string key, out AsyncOperationHandle<T> handle) where T : Object
        {
            key = (string) aRef?.RuntimeKey ?? key;

            // if given asset is already loaded
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

            // if given asset currently in a loading process
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

            // a given asset doesn't exist and should be loaded
            handle = aRef?.LoadAssetAsync<T>() ?? Addressables.LoadAssetAsync<T>(key);

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
    }
}