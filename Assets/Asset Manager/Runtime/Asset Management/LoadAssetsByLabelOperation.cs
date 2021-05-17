using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Skywatch.AssetManagement
{
    //Special thanks to TextusGames for their forum post: https://forum.unity.com/threads/how-to-get-asset-and-its-guid-from-known-lable.756560/
    // TODO: Try to pool and reuse this class with all internal dynamically created structures
    // TODO: Add Reset for nullifying 
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