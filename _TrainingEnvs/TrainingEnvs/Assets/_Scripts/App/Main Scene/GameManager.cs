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

    [Header("Input editare Helios - Slidere")]
    [Tooltip("Slider MaxMulakAgents")] [SerializeField] Slider[] heliosParameterSliders = null;
    [Tooltip("Slider Text Value")] [SerializeField] Text[] heliosParametersTexts = null;

    [Header("Input editare Mulak - Slidere")]
    [Tooltip("Slider MaxMulakAgents")] [SerializeField] Slider[] mulakParameterSliders = null;
    [Tooltip("Slider Text Value")] [SerializeField] Text[] mulakParametersTexts = null;

    [Header("Input editare Phaoris - Slidere")]
    [Tooltip("Slider MaxMulakAgents")] [SerializeField] Slider[] phaorisParameterSliders = null;
    [Tooltip("Slider Text Value")] [SerializeField] Text[] phaorisParametersTexts = null;


    [Header("Camera din spatiul de simulare")]
    [Tooltip("Camera")] [SerializeField] FlyingCamera simulationAreaCamera = null;

    [Header("Buton")]
    [Tooltip("Butonul de resume a simularii")] [SerializeField] Button resumeButton = null;

    // -------- STRUCTURI ------- //

    public AgentParameters HeliosParameters;
    public AgentParameters MulakParameters;
    public AgentParameters PhaorisParameters;

    // -------- VARIABILE ------- //

    // Numarul maxim de agenti Mulak permis
    float mulakMaxAgentsNumberValue = 0;
    public float GetMulakMaxAgentsNumberValue() { return mulakMaxAgentsNumberValue; }

    // Transformul actionbar-ului (element GUI)
    Transform actionBar = null;

    // Transformul statisticilor simularii (element GUI)
    Transform statisticsOutput = null;

    // Transformul butonului de back in placement mode
    Transform backButtonInPlacementMode = null;

    // Transformul butonului de back to main menu
    Transform backToMainMenuButton = null;

    // Transformul butonului de amplasare agenti
    Transform placeAgentsButton = null;

    // Transformul parametrilor de editare 
    Transform parameterEditorLayout = null;

    bool raysEnabled = false;
    public bool GetRaysEnabled() { return raysEnabled; }

    // -------- VARIABILE TIMER ------- //

    // Boolean ce dicteaza daca timerul este activat
    bool activeTimer = false;
    // Timpul in app pana cand se da drumul la simulare
    float startSimTime = 0f;
    // Durata simularii
    float simDuration = 0f;
    public float GetSimDuration() { return simDuration; }

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

        // Initializam si dezactivam buton de back in placement mode
        backButtonInPlacementMode = canvas.transform.Find("BackToPlacementButton");

        // Initializare parameter editor layout
        parameterEditorLayout = canvas.transform.Find("ParameterEditorLayout");

        // Initializare buton place agents
        placeAgentsButton = canvas.transform.Find("PlaceAgentsButton");

        // Initializare buton back to main menu
        backToMainMenuButton = canvas.transform.Find("BackToMainMenuButton");

        // Initializam afisarea statisticilor
        statisticsOutput = canvas.transform.Find("StatisticsOutput");

        // Constructor parametri agenti
        HeliosParameters = new AgentParameters();
        MulakParameters = new AgentParameters();
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

        // Verificam daca editam in timp ce amplasam agentii
        if(GetSceneState == 1) EditWhilePlacing();

        // Update text slider Helios
        for (int i = 0; i < heliosParameterSliders.Length; i++)
            heliosParametersTexts[i].text = heliosParameterSliders[i].value.ToString();

        // Update text slider Mulak
        for (int i = 0; i < mulakParameterSliders.Length; i++) 
            mulakParametersTexts[i].text = mulakParameterSliders[i].value.ToString();

        // Update text slider Phaoris
        for (int i = 0; i < phaorisParameterSliders.Length; i++)
            phaorisParametersTexts[i].text = phaorisParameterSliders[i].value.ToString();

        // Daca timer-ul este activ il folosim
        if (activeTimer)
            simDuration = Time.time - startSimTime;
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
        if (Input.GetKeyDown(KeyCode.P) && GetSceneState == 1)
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

            // Seteaza timpul in care incepe simularea
            startSimTime = Time.time;
            activeTimer = true;
        }
    }

    // Edit while placing
    private void EditWhilePlacing()
    {
        // Activate agents after placement
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Interzicem miscarea camerei
            simulationAreaCamera.CanMoveCamera = false;

            GetSceneState = 3;

            // Dezactivam bara de selectare a agentilor
            EnableOrDisableElement(actionBar, false);

            // Activam doar parametrii de editare si butonul de back to placement
            EnableOrDisableElement(parameterEditorLayout, true);
            EnableOrDisableElement(backButtonInPlacementMode, true);

            // Deblocam mouse-ul
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Interzicem amplasarea agentilor
            PlacementController.Instance.CanPlaceAgents = false;

            // Distrugem agent ce urmeaza sa fie amplasat daca am inceput simularea
            PlacementController.Instance.DestroyCurrentPlaceableObject();
        }
    }

    // Edit while placing
    public void BackToPlacementButton()
    {
        // Permitem miscarea camerei
        simulationAreaCamera.CanMoveCamera = true;

        SetParametersBeforePlacing();

        GetSceneState = 1;

        // Dezactivam bara de selectare a agentilor
        EnableOrDisableElement(actionBar, true);

        // Dezactivam doar parametrii de editare si butonul de back to placement
        EnableOrDisableElement(parameterEditorLayout, false);
        EnableOrDisableElement(backButtonInPlacementMode, false);

        // Deblocam mouse-ul
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Interzicem amplasarea agentilor
        PlacementController.Instance.CanPlaceAgents = true;
    }

    // Metoda schimbare zone
    public void PlaceAgentsButton()
    {
        // Ne mutam in zona de simulare
        GetSceneState = 1;

        // Dezactivam zona de editare (GameObjects + UI)
        EnableOrDisableElement(editArea, false);
        EnableOrDisableElement(parameterEditorLayout, false);
        EnableOrDisableElement(backToMainMenuButton, false);
        EnableOrDisableElement(placeAgentsButton, false);

        // Activam zona de simulare (GameObjects + UI)
        EnableOrDisableElement(actionBar, true);
        EnableOrDisableElement(simulationArea, true);

        // Permitem amplasarea agentilor 
        PlacementController.Instance.CanPlaceAgents = true;
        // Nu le permitem sa ia decizii
        CanAgentsRequestDecisions = false;

        // Blocam mouse-ul
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Permitem miscarea camerei
        simulationAreaCamera.CanMoveCamera = true;

        // Setam parametrii agentilor
        SetParametersBeforePlacing();

        // Resetam datele pentru statistici
        StatisticsManager.Instance.SetInitialAgentNumbers("reset");
        StatisticsManager.Instance.ModifySimData("reset");
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
            Cursor.visible = true;
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
            Cursor.visible = true;
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
        Cursor.visible = true;
        // Blocam camera
        simulationAreaCamera.CanMoveCamera = false;

        // Blocam butonul de resume simulare
        resumeButton.interactable = false;

        // oprim timer-ul
        activeTimer = false;
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
        Cursor.visible = false;
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
        // Dezactivam afisarea statisticilor
        EnableOrDisableElement(statisticsOutput, false);
        // Deblocam mouse-ul
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Deblocam butonul de resume simulare
        resumeButton.interactable = true;

        // oprim timer-ul
        activeTimer = false;

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
        EnableOrDisableElement(parameterEditorLayout, true);
        EnableOrDisableElement(backToMainMenuButton, true);
        EnableOrDisableElement(placeAgentsButton, true);

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
        HeliosParameters.MoveSpeed = heliosParameterSliders[0].value;
        HeliosParameters.RotationSpeed = heliosParameterSliders[1].value;
        HeliosParameters.SearchProximity = heliosParameterSliders[2].value;

        // Infometare
        HeliosParameters.starvingInterval = heliosParameterSliders[3].value;

        // ------------------------------- Setare parametri Mulak ------------------------------------ // 

        // Deplasare
        MulakParameters.DashForce = mulakParameterSliders[0].value;
        MulakParameters.DashCooldown = mulakParameterSliders[1].value;
        MulakParameters.RotationSpeed = mulakParameterSliders[2].value;
        MulakParameters.SearchProximity = mulakParameterSliders[3].value;

        // Imperechere
        MulakParameters.MateProximity = mulakParameterSliders[4].value;

        // Infometare
        MulakParameters.starvingInterval = mulakParameterSliders[5].value;

        // Setam numarul maxim de agenti Mulak
        mulakMaxAgentsNumberValue = mulakParameterSliders[6].value;

        // ------------------------------- Setare parametri Phaoris ------------------------------------ //

        PhaorisParameters.MoveSpeed = phaorisParameterSliders[0].value;
        PhaorisParameters.Y_RotationSpeed = phaorisParameterSliders[1].value;
        PhaorisParameters.X_RotationSpeed = phaorisParameterSliders[2].value;
        PhaorisParameters.SearchProximity = phaorisParameterSliders[3].value;
        PhaorisParameters.DeliveryDistance = phaorisParameterSliders[4].value;
    }
}
