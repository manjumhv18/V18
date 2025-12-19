using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ExperimentLoader : MonoBehaviour
{
    [Header("Container where the experiment prefab will be instantiated")]
    [SerializeField] private RectTransform panelContainer;

    private GameObject _currentInstance;
    private AsyncOperationHandle<GameObject>? _currentHandle;

    // Call this with the selected experiment key (e.g., "SolarSystem/OrbitDemo")
    public async Task LoadExperimentAsync(string addressKey)
    {
        await InitializeAddressablesIfNeeded();

        // Clean previous
        UnloadCurrent();

        var handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
        _currentHandle = handle;

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"Failed to load experiment: {addressKey}");
            _currentHandle = null;
            return;
        }

        var prefab = handle.Result;
        _currentInstance = Instantiate(prefab, panelContainer);

        // Optional: ensure full-size in panel for UI prefabs
        var rt = _currentInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public void UnloadCurrent()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        if (_currentHandle.HasValue)
        {
            Addressables.Release(_currentHandle.Value);
            _currentHandle = null;
        }
    }

    private static bool _init;
    private static Task _initTask;

    private static async Task InitializeAddressablesIfNeeded()
    {
        if (_init) return;
        _initTask ??= Addressables.InitializeAsync().Task;
        await _initTask;
        _init = true;
    }
}