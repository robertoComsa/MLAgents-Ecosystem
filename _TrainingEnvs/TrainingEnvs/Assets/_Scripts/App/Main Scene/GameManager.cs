using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Input editare Mulak - Slider")]
    [Tooltip("Slider MaxMulakAgents")] [SerializeField] Slider mulakMaxAgentsNumberSlider = null;
    [Tooltip("Slider Text Value")] [SerializeField] Text mulakMaxAgentsNumberText = null;

    [Header("Camera din spatiul de simulare")]
    [Tooltip("Camera")] [SerializeField] FlyingCamera simulationAreaCamera = null;

    [Header("Buton")]
    [Tooltip("Butonul de resume a simularii")] [SerializeField] Button resumeButton = null;

    // -------- STRUCTURI ------- //

    public AgentParameters HeliosParameters;
    public AgentParameters MulakParameters;
    public AgentParameters GalvadonParameters;
    public AgentParameters PhaorisParameters;

    // -------- VARIABILE ------- //

    // Numarul maxim de agenti Mulak permis
    float mulakMaxAgentsNumberValue = 0;
    public float GetMulakMaxAgentsNumberValue() { return mulakMaxAgentsNumberValue; }

    // Transformul actionbar-ului (element GUI)
    Transform actionBar = null;

    // Transformul meniului de editare (element GUI)
    Transform parameterEditorMenu = null;

    // Transformul statisticilor simularii (element GUI)
    Transform statisticsOutput = null;

    bool raysEnabled = true;
    public bool GetRaysEnabled() { return raysEnabled; }

    // ------ PROPRIETATI ------- //

    public bool CanAgentsRequestDecisions { get; set; } = false;

    public int GetSceneState { get; set; } = 0; // 0 - Zona de editare , 1 - Amplasarea agentilor , 2 - Simulare 

    public bool SimulationEnded { get; set; } = false;

    public bool gamePaused { get; set; } = false;

    // --------------------------------------------------------------- METODE SISTEM ------------------------------------------------------------------- //

    // Back button from PrepareSimulation
    public void BackToMainMenu() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1); }

    // Prima metoda apelata ( o singura data )
    protected override void Awake()
    {
        base.Awake();
        
        // Initializam si dezactivam action bar-ul
        actionBar = canvas.transform.Find("Actionbar");
        simulationArea.gameObject.SetActive(false);

        // Initializam meniul de editare
        parameterEditorMenu = canvas.transform.Find("ParameterEditorMenu");

        // Initializam afisarea statisticilor
        statisticsOutput = canvas.transform.Find("StatisticsOutput");

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

        // Verificam in fiecare frame daca punem pauza la simulare
        PauseOnEscape();

        // Verificam in fiecare frame daca apasam r (pentru activare/dezactivare rays)
        EnableOrDisableRays();

        // Update text slider
        mulakMaxAgentsNumberText.text = mulakMaxAgentsNumberSlider.value.ToString();
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
            // Agenti pot lua decizii 
            CanAgentsRequestDecisions = true;

            // Dezactivam bara de selectare a agentilor
            EnableOrDisableElement(actionBar, false);

            // Tranzitionam starea scenei
            GetSceneState = 2;

            // Interzicem amplasarea agentilor
            PlacementController.Instance.CanPlaceAgents = false;

            // Setam numarul initial de agenti (pentru statistici)
            StatisticsManager.Instance.SetInitialAgentNumbers("set");

            // Distrugem agent ce urmeaza sa fie amplasat daca am inceput simularea
            PlacementController.Instance.DestroyCurrentPlaceableObject();
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

        // Permitem miscarea camerei
        simulationAreaCamera.CanMoveCamera = true;

        // Setam parametrii agentilor
        SetParametersBeforePlacing();

        // Resetam datele pentru statistici
        StatisticsManager.Instance.SetInitialAgentNumbers("reset");
        StatisticsManager.Instance.ModifySimData("reset");

        // Setam numarul maxim de agenti Mulak
        mulakMaxAgentsNumberValue = mulakMaxAgentsNumberSlider.value;
    }

    // <>--<> GESTIONARE SIMULARE <>--<>

    // Metoda de pauza a simularii 
    public void PauseOnEscape()
    {
        if (GetSceneState == 2 && Input.GetKeyDown(KeyCode.Escape))
        {
            // Agentii nu mai pot lua decizii
            CanAgentsRequestDecisions = false;
            // Afisam statisticile
            EnableOrDisableElement(statisticsOutput, true);
            // Setam corect statisticile
            StatisticsManager.Instance.SetSimDataTxt();
            // Deblocam mouse-ul
            Cursor.lockState = CursorLockMode.None;
            // Blocam camera
            simulationAreaCamera.CanMoveCamera = false;
            // Punem pauza 
            gamePaused = true;
            // Inghetam timpul pentru a opri Coroutinele Agentilor
            Time.timeScale = 0;
        }
        else if(GetSceneState == 1 && Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(DestroyAgentsThenSwapScene(0.1f));
            // Deblocam mouse-ul
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Metoda enable/disable rays
    public void EnableOrDisableRays()
    {
        if (GetSceneState == 2 && Input.GetKeyDown(KeyCode.R))
            raysEnabled = !raysEnabled;
    }

    // Incheie simularea atunci cand nu mai sunt agenti
    public void EndSimulationOnAgentsDeath()
    {
        // Agentii nu mai pot lua decizii
        CanAgentsRequestDecisions = false;
        // Afisam statisticile
        EnableOrDisableElement(statisticsOutput, true);
        // Setam corect statisticile
        StatisticsManager.Instance.SetSimDataTxt();
        // Deblocam mouse-ul
        Cursor.lockState = CursorLockMode.None;
        // Blocam camera
        simulationAreaCamera.CanMoveCamera = false;

        // Blocam butonul de resume simulare
        resumeButton.interactable = false;
    }

    // Metoda de reluare a simularii
    public void ResumeSimulationButton()
    {
        // Agentii pot lua decizii
        CanAgentsRequestDecisions = true;
        // Dezactivam afisarea statisticilor
        EnableOrDisableElement(statisticsOutput, false);
        // Blocam mouse-ul
        Cursor.lockState = CursorLockMode.Locked;
        // Permitem miscarea camerei
        simulationAreaCamera.CanMoveCamera = true;
        // Incheiem pauza
        gamePaused = false;
        // Dezghetam timpul pentru a opri Coroutinele 
        Time.timeScale = 1;
    }

    // Metoda de incheiere a simularii ce ne intoarce l 
    public void EndSimulationButton()
    {
        // Dezghetam timpul pentru a opri Coroutinele 
        Time.timeScale = 1;
        // Eliminam agentii din scena
        StartCoroutine(DestroyAgentsThenSwapScene(0.1f));
        // Deblocam mouse-ul
        Cursor.lockState = CursorLockMode.None;
        // Dezactivam afisarea statisticilor
        EnableOrDisableElement(statisticsOutput, false);
        // Deblocam mouse-ul
        Cursor.lockState = CursorLockMode.None;
        // Deblocam butonul de resume simulare
        resumeButton.interactable = true;

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
        // ----------------------------- Setare parametri Helios ---------------------------------------- //

        // Deplasare
        HeliosParameters.MoveSpeed = int.Parse(heliosParametersText[0].text);
        HeliosParameters.RotationSpeed = int.Parse(heliosParametersText[1].text);
        HeliosParameters.SearchProximity = int.Parse(heliosParametersText[2].text);

        // Infometare
        HeliosParameters.hungerFactor = int.Parse(heliosParametersText[3].text);
        HeliosParameters.hungerTickValue = int.Parse(heliosParametersText[4].text);
        HeliosParameters.timeBetweenHungerTicks = int.Parse(heliosParametersText[5].text);

        // ------------------------------- Setare parametri Mulak ------------------------------------ // 

        // Deplasare
        MulakParameters.MoveSpeed = int.Parse(mulakParametersText[0].text);
        MulakParameters.RotationSpeed = int.Parse(mulakParametersText[1].text);
        MulakParameters.SearchProximity = int.Parse(mulakParametersText[2].text);

        // Imperechere
        MulakParameters.MateProximity = int.Parse(mulakParametersText[3].text);

        // Infometare
        MulakParameters.hungerFactor = int.Parse(mulakParametersText[4].text);
        MulakParameters.hungerTickValue = int.Parse(mulakParametersText[5].text);
        MulakParameters.timeBetweenHungerTicks = int.Parse(mulakParametersText[6].text);

        // ------------------------------- Setare parametri Galvadon ---------------------------------- // 

        // Deplasare
        GalvadonParameters.MoveSpeed = int.Parse(galvadonParametersText[0].text);
        GalvadonParameters.RotationSpeed = int.Parse(galvadonParametersText[1].text);
        GalvadonParameters.SearchProximity = int.Parse(galvadonParametersText[2].text);

        // Infometare
        GalvadonParameters.hungerFactor = int.Parse(galvadonParametersText[3].text);
        GalvadonParameters.hungerTickValue = int.Parse(galvadonParametersText[4].text);
        GalvadonParameters.timeBetweenHungerTicks = int.Parse(galvadonParametersText[5].text);

        // ------------------------------- Setare parametri Phaoris ------------------------------------ //

        PhaorisParameters.MoveSpeed = int.Parse(phaorisParametersText[0].text);
        PhaorisParameters.Y_RotationSpeed = int.Parse(phaorisParametersText[1].text);
        PhaorisParameters.X_RotationSpeed = int.Parse(phaorisParametersText[2].text);
        PhaorisParameters.SearchProximity = int.Parse(phaorisParametersText[3].text);
        PhaorisParameters.DeliveryDistance = int.Parse(phaorisParametersText[4].text);
    }
}
