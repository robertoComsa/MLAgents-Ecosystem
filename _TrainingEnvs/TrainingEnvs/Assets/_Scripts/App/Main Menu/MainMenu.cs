using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAgents;

public class MainMenu : MonoBehaviour
{

    // Prepare simulation
    public void LoadPrepareSimulation()
    {
        Academy.Instance.Dispose();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Quit application
    public void QuitApplication()
    {
        Application.Quit();
    }
}
