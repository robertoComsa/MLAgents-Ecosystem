using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    // ---- VARIABILE VIZIBILE IN EDITOR ---- //

    [Header("Canvas")]
    [Tooltip("GameObjectul canvas")][SerializeField] Canvas canvas = null;

    [Header("Spatii")]
    [Tooltip("Spatiul de editare")] [SerializeField] Transform editArea = null;
    [Tooltip("Spatiul de simulare")] [SerializeField] Transform simulationArea = null;

    [Header("Input editare agenti")]
    [Tooltip("Parametri Helios")] [SerializeField] Text[] heliosParametersText = null;
    [Tooltip("Parametri Mulak")] [SerializeField] Text[] mulakParametersText = null;
    [Tooltip("Parametri Galvadon")] [SerializeField] Text[] galvadonParametersText = null;
    [Tooltip("Parametri Phaoris")] [SerializeField] Text[] phaorisParametersText = null;

    // -------- SRTRUCTURI ------- //

    public AgentParameters HeliosParameters;
    public AgentParameters MulakParameters;
    public AgentParameters GalvadonParameters;
    public AgentParameters PhaorisParameters;
       
    // Transformul actionbar-ului (element GUI)
    Transform actionBar = null;

    // Transformul meniului de editare
    Transform parameterEditorMenu = null;

    // ------ PROPRIETATI ------- //

    public bool CanAgentsRequestDecisions { get; set; } = false;

    public int GetSceneState { get; set; } = 0; // 0 - Zona de editare , 1 - Zona de simulare

    public bool SimulationEnded { get; set; } = false;

    // --------------------------------------------------------------- METODE SISTEM ------------------------------------------------------------------- //

    // Prima metoda apelata ( o singura data )
    protected override void Awake()
    {
        base.Awake();
        
        // Initializam si dezactivam action bar-ul
        actionBar = canvas.transform.Find("Actionbar");
        simulationArea.gameObject.SetActive(false);

        // Initializam meniul de editare
        parameterEditorMenu = canvas.transform.Find("ParameterEditorMenu");

        // Constructor parametri agenti
        HeliosParameters = new AgentParameters();
        MulakParameters = new AgentParameters();
        GalvadonParameters = new AgentParameters();
        PhaorisParameters = new AgentParameters();
    }

    // Apelata in fiecare frame
    private void Update()
    {
        // Verificam in fiecare frame daca dam drumul la simulare
        StartSimulation();

        // Verificam in fiecare frame daca incheiem simularea si revenim la meniul de editare
        EndSimulation();
    }

    // --------------------------------------------------------------------- METODE ------------------------------------------------------------------- //
    
    // <>--<> ADMINISTRARE SCENE & UI <>--<>

    // Metoda folosita la activarea / dezactivarea elementelor GUI din scena
    private void EnableOrDisableElement(Transform element , bool state)
    {
        element.gameObject.SetActive(state);
    }

    // START SIMULATION
    private void StartSimulation()
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
    public void PlaceAgentsButton()
    {
        // Ne mutam in zona de simulare
        GetSceneState = 1;

        // Dezactivam zona de editare (GameObjects + UI)
        EnableOrDisableElement(editArea, false);
        EnableOrDisableElement(parameterEditorMenu, false);

        // Activam zona de simulare (GameObjects + UI)
        EnableOrDisableElement(actionBar, true);
        EnableOrDisableElement(simulationArea, true);

        // Permitem amplasarea agentilor 
        PlacementController.Instance.CanPlaceAgents = true;
        // Nu le permitem sa ia decizii
        CanAgentsRequestDecisions = false;

        // Blocam mouse-ul
        Cursor.lockState = CursorLockMode.Locked;

        // Setam parametrii agentilor
        SetParametersBeforePlacing();
    }

    // Metoda de incheiere a simularii ce ne intoarce l 
    private void EndSimulation()
    {
        if (GetSceneState == 1 && Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(DestroyAgentsThenSwapScene(0.1f));
            // Deblocam mouse-ul
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Rutina ce distruge agentii inainte de a dezactiva scena
    IEnumerator DestroyAgentsThenSwapScene(float value)
    {
        // Anuntam agentii ca simularea s-a terminat
        SimulationEnded = true;

        // Asteptam (0.1s) ca agentii sa se distruga automat
        yield return new WaitForSeconds(value);

        // Resetam variabila simulation ended
        SimulationEnded = false;

        // Ne mutam in zona de editare
        GetSceneState = 0;

        // Activam zona de editare (GameObjects + UI)
        EnableOrDisableElement(editArea, true);
        EnableOrDisableElement(parameterEditorMenu, true);

        // Dezactivam zone de simulare (GameObjects + UI)
        EnableOrDisableElement(simulationArea, false);
        EnableOrDisableElement(actionBar, false);

        // Interzicem amplasarea agentilor 
        PlacementController.Instance.CanPlaceAgents = false;
    }

    // <>--<> INCARCARE INPUT DIN ZONA DE EDITARE <>--<>

    // Metoda care seteaza parametrii agentilor atunci cand ne mutam in zona de simulare
    private void SetParametersBeforePlacing()
    {
        // Setare parametri Helios
        HeliosParameters.MoveSpeed = int.Parse(heliosParametersText[0].text);
        HeliosParameters.RotationSpeed = int.Parse(heliosParametersText[1].text);
        HeliosParameters.SearchProximity = int.Parse(heliosParametersText[2].text);

        // Setare parametri Mulak
        MulakParameters.MoveSpeed = int.Parse(mulakParametersText[0].text);
        MulakParameters.RotationSpeed = int.Parse(mulakParametersText[1].text);
        MulakParameters.SearchProximity = int.Parse(mulakParametersText[2].text);
        MulakParameters.MateProximity = int.Parse(mulakParametersText[3].text);

        // Setare parametri Galvadon
        GalvadonParameters.MoveSpeed = int.Parse(galvadonParametersText[0].text);
        GalvadonParameters.RotationSpeed = int.Parse(galvadonParametersText[1].text);
        GalvadonParameters.SearchProximity = int.Parse(galvadonParametersText[2].text);

        //Setare parametri Phaoris
        PhaorisParameters.MoveSpeed = int.Parse(phaorisParametersText[0].text);
        PhaorisParameters.Y_RotationSpeed = int.Parse(phaorisParametersText[1].text);
        PhaorisParameters.X_RotationSpeed = int.Parse(phaorisParametersText[2].text);
        PhaorisParameters.SearchProximity = int.Parse(phaorisParametersText[3].text);
        PhaorisParameters.DeliveryDistance = int.Parse(phaorisParametersText[4].text);
    }
}
