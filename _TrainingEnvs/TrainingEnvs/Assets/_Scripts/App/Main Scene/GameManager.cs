using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // ---- VARIABILE VIZIBILE IN EDITOR ---- //

    [Tooltip("GameObjectul canvas")][SerializeField] Canvas canvas = null;

    [Header("Spatii")]
    [Tooltip("Spatiul de editare")] [SerializeField] Transform editArea = null;
    [Tooltip("Spatiul de simulare")] [SerializeField] Transform simulationArea = null;

    // -------- VARIABILE ------- //

    // Transformul actionbar-ului (element GUI)
    Transform actionBar = null;

    // ------ PROPRIETATI ------- //

    public bool CanAgentsRequestDecisions { get; set; } = false;

    public int GetSceneState { get; set; } = 0; // 0 - Zona de editare , 1 - Zona de simulare

    public bool SimulationEnded { get; set; } = false;

    // --------------------------------------------------------------- METODE SISTEM ------------------------------------------------------------------- //

    // Prima metoda apelata ( o singura data )
    protected override void Awake()
    {
        base.Awake();
        actionBar = canvas.transform.Find("Actionbar");
        simulationArea.gameObject.SetActive(false);
    }

    // Apelata in fiecare frame
    private void Update()
    {
        // Metoda administrare GUI 
        ManageGUI();

        // Metoda de schimbare a zonei
        SwitchArea();
    }

    // --------------------------------------------------------------------- METODE ------------------------------------------------------------------- //
    
    // Metoda folosita la activarea / dezactivarea elementelor GUI din scena
    private void EnableOrDisableElement(Transform element , bool state)
    {
        element.gameObject.SetActive(state);
    }

    // Metoda administrare GUI 
    private void ManageGUI()
    {
        // Activate agents after placement
        if (Input.GetKeyDown(KeyCode.M) && GetSceneState == 1)
        {
            CanAgentsRequestDecisions = true;
            EnableOrDisableElement(actionBar, false);

            // Interzicem amplasarea agentilor
            PlacementController.Instance.CanPlaceAgents = false;
        }
    }

    // Metoda schimbare zone
    private void SwitchArea()
    {
        // Momentan schimb prin apasarea tastei escape. 
        if(Input.GetKeyDown(KeyCode.P))
        {
            if (GetSceneState == 0)
            {
                GetSceneState = 1;
                EnableOrDisableElement(simulationArea, true);
                EnableOrDisableElement(editArea, false);
                EnableOrDisableElement(actionBar, true);

                // Permitem amplasarea agentilor 
                PlacementController.Instance.CanPlaceAgents = true;
                // Nu le permitem sa ia decizii
                CanAgentsRequestDecisions = false;
            }
            else
            {
                StartCoroutine(DestroyAgentsThenSwapScene(0.1f));
            }
        }
    }

    // Rutina ce distruge agentii inainte de a dezactiva scena
    IEnumerator DestroyAgentsThenSwapScene(float value)
    {
        SimulationEnded = true;

        yield return new WaitForSeconds(value);

        SimulationEnded = false;
        GetSceneState = 0;
        EnableOrDisableElement(simulationArea, false);
        EnableOrDisableElement(editArea, true);
        EnableOrDisableElement(actionBar, false);

        // Interzicem amplasarea agentilor 
        PlacementController.Instance.CanPlaceAgents = false;
    }

}
