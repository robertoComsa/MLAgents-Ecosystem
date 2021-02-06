using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MLAgents;

public class MainMenu : MonoBehaviour
{

    [Header("Loading slider")]
    [Tooltip("GameObject-ul parinte al sliderului")] [SerializeField] GameObject loadingSliderParent = null;
    [Tooltip("Sliderul")] [SerializeField] Slider loadingSlider = null;

    // Prepare simulation
    public void LoadPrepareSimulation()
    {
        loadingSliderParent.SetActive(true);
        Academy.Instance.Dispose();
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

        while(!operation.isDone)
        {
            float loadingProgress = Mathf.Clamp01(operation.progress / .9f);

            loadingSlider.value = loadingProgress; 

            yield return null;
        }
    }



    // Quit application
    public void QuitApplication()
    {
        Application.Quit();
    }
}
