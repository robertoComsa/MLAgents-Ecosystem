using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAgents;

public class MainMenu : MonoBehaviour
{
    public void LoadPrepareSimulation()
    {
        Academy.Instance.Dispose();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}
