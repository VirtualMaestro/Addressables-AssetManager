using System.Collections.Generic;

namespace UnityEngine.AddressableAssets
{
    public static partial class AssetManager
    {
                // public static bool TryInstantiateOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef,
        //     Vector3 position, Quaternion rotation, Transform parent,
        //     out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        // {
        //     return TryInstantiateOrLoadAsync(aRef as AssetReference, position, rotation, parent, out handle);
        // }

        // #region Multi
        //
        // public static bool TryInstantiateMultiOrLoadAsync(AssetReference aRef, int count, Vector3 position,
        //     Quaternion rotation, Transform parent,
        //     out AsyncOperationHandle<List<GameObject>> handle)
        // {
        //     if (LoadAsset(aRef, out AsyncOperationHandle<GameObject> loadHandle))
        //     {
        //         var list = new List<GameObject>(count);
        //         for (var i = 0; i < count; i++)
        //         {
        //             var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
        //             list.Add(instance);
        //         }
        //
        //         handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
        //         return true;
        //     }
        //
        //     if (!loadHandle.IsValid())
        //     {
        //         Debug.LogError($"Load Operation was invalid: {loadHandle}.");
        //         handle = Addressables.ResourceManager.CreateCompletedOperation<List<GameObject>>(null,
        //             $"Load Operation was invalid: {loadHandle}.");
        //         return false;
        //     }
        //
        //     handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
        //     {
        //         var list = new List<GameObject>(count);
        //         for (int i = 0; i < count; i++)
        //         {
        //             var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
        //             list.Add(instance);
        //         }
        //
        //         return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
        //     });
        //     return false;
        // }
        //
        // public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReference aRef, int count,
        //     Vector3 position, Quaternion rotation, Transform parent,
        //     out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        // {
        //     if (InstantiateComponent(aRef, out AsyncOperationHandle<TComponentType> loadHandle))
        //     {
        //         var list = new List<TComponentType>(count);
        //         for (int i = 0; i < count; i++)
        //         {
        //             var instance = _InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
        //             list.Add(instance);
        //         }
        //
        //         handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
        //         return true;
        //     }
        //
        //     if (!loadHandle.IsValid())
        //     {
        //         Debug.LogError($"Load Operation was invalid: {loadHandle}.");
        //         handle = Addressables.ResourceManager.CreateCompletedOperation<List<TComponentType>>(null,
        //             $"Load Operation was invalid: {loadHandle}.");
        //         return false;
        //     }
        //
        //     handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
        //     {
        //         var list = new List<TComponentType>(count);
        //         for (var i = 0; i < count; i++)
        //         {
        //             var instance = _InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
        //             list.Add(instance);
        //         }
        //
        //         return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
        //     });
        //
        //     return false;
        // }
        //
        // public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef,
        //     int count, Vector3 position, Quaternion rotation,
        //     Transform parent, out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        // {
        //     return TryInstantiateMultiOrLoadAsync(aRef as AssetReference, count, position, rotation, parent,
        //         out handle);
        // }
        //
        // #endregion
        //
        // #region Sync
        //
        // public static bool TryInstantiateSync(AssetReference aRef, Vector3 position, Quaternion rotation,
        //     Transform parent, out GameObject result)
        // {
        //     if (!TryGetObjectSync(aRef, out GameObject loadResult))
        //     {
        //         result = null;
        //         return false;
        //     }
        //
        //     result = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
        //     return true;
        // }
        //
        // public static bool TryInstantiateSync<TComponentType>(AssetReference aRef, Vector3 position,
        //     Quaternion rotation, Transform parent,
        //     out TComponentType result) where TComponentType : Component
        // {
        //     if (!TryGetComponentSync(aRef, out TComponentType loadResult))
        //     {
        //         result = null;
        //         return false;
        //     }
        //
        //     result = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
        //     return true;
        // }
        //
        // public static bool TryInstantiateSync<TComponentType>(AssetReferenceT<TComponentType> aRef, Vector3 position,
        //     Quaternion rotation, Transform parent,
        //     out TComponentType result) where TComponentType : Component
        // {
        //     return TryInstantiateSync(aRef as AssetReference, position, rotation, parent, out result);
        // }
        //
        // public static bool TryInstantiateMultiSync(AssetReference aRef, int count, Vector3 position,
        //     Quaternion rotation, Transform parent, out List<GameObject> result)
        // {
        //     if (!TryGetObjectSync(aRef, out GameObject loadResult))
        //     {
        //         result = null;
        //         return false;
        //     }
        //
        //     var list = new List<GameObject>(count);
        //     for (int i = 0; i < count; i++)
        //     {
        //         // var instance = _InstantiateInternal(aRef, loadResult, position, rotation, parent);
        //         // list.Add(instance);
        //     }
        //
        //     result = list;
        //     return true;
        // }
        //
        // public static bool TryInstantiateMultiSync<TComponentType>(AssetReferenceT<TComponentType> aRef, int count,
        //     Vector3 position, Quaternion rotation, Transform parent,
        //     out List<TComponentType> result) where TComponentType : Component
        // {
        //     return TryInstantiateMultiSync(aRef as AssetReference, count, position, rotation, parent, out result);
        // }
        //
        // public static bool TryInstantiateMultiSync<TComponentType>(AssetReference aRef, int count, Vector3 position,
        //     Quaternion rotation, Transform parent,
        //     out List<TComponentType> result) where TComponentType : Component
        // {
        //     if (!TryGetComponentSync(aRef, out TComponentType loadResult))
        //     {
        //         result = null;
        //         return false;
        //     }
        //
        //     var list = new List<TComponentType>(count);
        //     for (int i = 0; i < count; i++)
        //     {
        //         var instance = _InstantiateComponentInternal(aRef, loadResult, position, rotation, parent);
        //         list.Add(instance);
        //     }
        //
        //     result = list;
        //     return true;
        // }
        //
        // public static bool TryGetObjectSync<TObjectType>(AssetReference aRef, out TObjectType result)
        //     where TObjectType : Object
        // {
        //     _CheckRuntimeKey(aRef);
        //     var key = (string) aRef.RuntimeKey;
        //
        //     if (LoadedAssets.ContainsKey(key))
        //     {
        //         result = LoadedAssets[key].Convert<TObjectType>().Result;
        //         return true;
        //     }
        //
        //     result = null;
        //     return false;
        // }
        //
        // public static bool TryGetObjectSync<TObjectType>(AssetReferenceT<TObjectType> aRef, out TObjectType result)
        //     where TObjectType : Object
        // {
        //     return TryGetObjectSync(aRef as AssetReference, out result);
        // }
        //
        // public static bool TryGetComponentSync<TComponentType>(AssetReference aRef, out TComponentType result)
        //     where TComponentType : Component
        // {
        //     _CheckRuntimeKey(aRef);
        //     var key = (string) aRef.RuntimeKey;
        //
        //     // TODO: Replace with TryGet
        //     if (LoadedAssets.ContainsKey(key))
        //     {
        //         var handle = LoadedAssets[key];
        //         result = null;
        //         var go = handle.Result as GameObject;
        //         if (!go)
        //             throw new ConversionException($"Cannot convert {nameof(handle.Result)} to {nameof(GameObject)}.");
        //         result = go.GetComponent<TComponentType>();
        //         if (!result)
        //             throw new ConversionException(
        //                 $"Cannot {nameof(go.GetComponent)} of Type {typeof(TComponentType)}.");
        //         return true;
        //     }
        //
        //     result = null;
        //     return false;
        // }
        //
        // public static bool TryGetComponentSync<TComponentType>(AssetReferenceT<TComponentType> aRef,
        //     out TComponentType result) where TComponentType : Component
        // {
        //     return TryGetComponentSync(aRef as AssetReference, out result);
        // }

    }
}