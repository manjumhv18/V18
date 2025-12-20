using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.XR;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExperimentSceneLoader : MonoBehaviour
{
    // Store the loaded scene handle
    private AsyncOperationHandle<SceneInstance>? loadedScene;

    /// <summary>
    /// Loads a scene additively by its Addressable name/address.
    /// </summary>
    /// <param name="sceneAddress">The Addressable name of the scene</param>
    /// 

    [Header("Reference scripts")]
    [SerializeField] ExperimentLogic experimentLogic;

    // Call this AFTER additive scene is loaded
    public void RegisterExperiment(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            experimentLogic = root.GetComponentInChildren<ExperimentLogic>();
            if (experimentLogic != null)
                break;
        }

        if (experimentLogic == null)
            Debug.LogError("ExperimentLogic not found in additive scene!");
    }

    // ===== UI Button hooks =====
    public void OnStep()
    {
        experimentLogic?.Step();
    }

    public void OnNext()
    {
        experimentLogic?.Next();
    }

    public void OnPrevious()
    {
        experimentLogic?.Previous();
    }

    public void OnReset()
    {
        experimentLogic?.ResetExperiment();
    }

    private void Start()
    {
        LoadExperiment("TestScene");
    }

    public void LoadExperiment(string sceneAddress)
    {
        // Unload previous scene if any
        UnloadExperiment();

        // Load the new scene additively
        loadedScene = Addressables.LoadSceneAsync(
            sceneAddress,
            LoadSceneMode.Additive,
            activateOnLoad: true
        );

        // Track when the scene is fully loaded
        loadedScene.Value.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Scene '{sceneAddress}' loaded successfully.");

                // ================= NEW CODE START =================
                Scene loadedUnityScene = handle.Result.Scene;

                RegisterExperiment(loadedUnityScene);
            }
            else
            {
                Debug.LogError($"Failed to load scene '{sceneAddress}'.");
            }
        };
    }

    /// <summary>
    /// Unloads the previously loaded experiment scene.
    /// </summary>
    public void UnloadExperiment()
    {
        if (loadedScene.HasValue)
        {
            Addressables.UnloadSceneAsync(loadedScene.Value).Completed += handle =>
            {
                Debug.Log("Previous experiment scene unloaded.");
            };
            loadedScene = null;
        }
    }
}
