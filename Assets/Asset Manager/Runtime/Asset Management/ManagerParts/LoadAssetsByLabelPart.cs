using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets
{
    public static partial class AssetManager
    {
        public static AsyncOperationHandle<List<AsyncOperationHandle<Object>>> LoadAssetsByLabelAsync(string label)
        {
            var op = new LoadAssetsByLabelOperation(label, LoadedAssets, LoadingAssets, 
                (key, handle) => OnAssetLoaded?.Invoke(key, handle));
            
            return Addressables.ResourceManager.StartOperation(op, default);
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
    }
    
    /// <summary>
    /// Operation class for loading assets by given label.
    /// </summary>
    public class LoadAssetsByLabelOperation : AsyncOperationBase<List<AsyncOperationHandle<Object>>>
    {
        private string _label;
        private Dictionary<string, AsyncOperationHandle> _loadedDictionary;
        private Dictionary<string, AsyncOperationHandle> _loadingDictionary;
        private Action<string, AsyncOperationHandle> _loadedCallback;

        public LoadAssetsByLabelOperation(string label, Dictionary<string, AsyncOperationHandle> loadedDictionary,
            Dictionary<string, AsyncOperationHandle> loadingDictionary,
            Action<string, AsyncOperationHandle> loadedCallback)
        {
            _loadedDictionary = loadedDictionary;
            _loadingDictionary = loadingDictionary;
            _loadedCallback = loadedCallback;
            _label = label;
        }

        // TODO: See what this compiler code means and remove if no needs
        protected override void Execute()
        {
            #pragma warning disable CS4014
            _DoTask();
            #pragma warning restore CS4014
        }

        // TODO: Optimize retrieving and validating ResourceLocators
        private async Task _DoTask()
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(_label);
            var locations = await locationsHandle.Task;

            var loadingInternalIdDic = new Dictionary<string, AsyncOperationHandle<Object>>();
            var loadedInternalIdDic = new Dictionary<string, AsyncOperationHandle<Object>>();
            var operationHandles = new List<AsyncOperationHandle<Object>>();
            
            foreach (var resourceLocation in locations)
            {
                var loadingHandle = Addressables.LoadAssetAsync<Object>(resourceLocation.PrimaryKey);

                operationHandles.Add(loadingHandle);

                if (!loadingInternalIdDic.ContainsKey(resourceLocation.InternalId))
                    loadingInternalIdDic.Add(resourceLocation.InternalId, loadingHandle);

                loadingHandle.Completed += assetOp =>
                {
                    if (!loadedInternalIdDic.ContainsKey(resourceLocation.InternalId))
                        loadedInternalIdDic.Add(resourceLocation.InternalId, assetOp);
                };
            }
            
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var keyObj in locator.Keys)
                {
                    var key = keyObj.ToString();
                    
                    if (!Guid.TryParse(key, out _) || 
                        !_TryGetKeyLocationID(locator, key, out var keyLocationID) || 
                        !loadingInternalIdDic.TryGetValue(keyLocationID, out var loadingHandle))
                        continue;
                    
                    if (!_loadingDictionary.ContainsKey(key))
                        _loadingDictionary.Add(key, loadingHandle);
                }
            }

            foreach (var handle in operationHandles)
                await handle.Task;

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var keyObj in locator.Keys)
                {
                    var key = keyObj.ToString();
                    
                    if (!Guid.TryParse(key, out _) || 
                        !_TryGetKeyLocationID(locator, key, out var keyLocationID) || 
                        !loadedInternalIdDic.TryGetValue(keyLocationID, out var loadedHandle))
                        continue;

                    if (_loadingDictionary.ContainsKey(key))
                        _loadingDictionary.Remove(key);
                    
                    if (!_loadedDictionary.ContainsKey(key))
                    {
                        _loadedDictionary.Add(key, loadedHandle);
                        _loadedCallback?.Invoke(key, loadedHandle);
                    }
                }
            }

            Complete(operationHandles, true, string.Empty);
            
            Dispose();
        }

        public void Dispose()
        {
            _loadedDictionary = null;
            _loadingDictionary = null;
            _loadedCallback = null;
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
    }
}