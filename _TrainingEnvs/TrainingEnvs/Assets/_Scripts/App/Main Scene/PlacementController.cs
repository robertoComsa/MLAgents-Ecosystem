using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PlacementController : Singleton<PlacementController>
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametri amplasare agenti in scena")]
    [Tooltip("Prefaburi agenti")] [SerializeField] GameObject[] placeableAgentsPrefabs = null;
    [Tooltip("Taste folosite pentru selectarea agentilor")] [SerializeField] KeyCode[] placeAgentHotkey = null;
    [Tooltip("Viteza de rotire")] [SerializeField] float mouseWheelRotationSpeed = 10f;
    [Tooltip("Transformul parinte")] [SerializeField] Transform parentTransform = null;

    //  ---------------------------------------------------------- VARIABILE ----------------------------------------------------- //

    GameObject currentPlaceableObject;
    int agentsNumber = 0;
    float mouseWheelRotation = 0f;

    // Daca putem plasa agenti sau nu
    public bool CanPlaceAgents{ get; set; } = true;

    // Agentul care este plasat
    private string agentInstantiated = "";


    // ------------------------------------------------------------- METODE ------------------------------------------------------- // 

    protected override void Awake()
    {
        base.Awake();
        agentsNumber = placeableAgentsPrefabs.Length;
    }

    private void Update()
    {
        if(CanPlaceAgents) HandleNewObjectHotkey();

        if (currentPlaceableObject != null)
        {
            MovePlaceableObjectToMouse();
            RotateTroughMouseWheel();
            OnClickRelease();
        }
    }


    // Metoda care selecteaza agentul ales pentru amplasare
    void HandleNewObjectHotkey()
    {
        for(int i=0;i<agentsNumber;i++)
        {
            if (Input.GetKeyDown(placeAgentHotkey[i]))
            {
                if (currentPlaceableObject == null)
                {
                    currentPlaceableObject = Instantiate(placeableAgentsPrefabs[i], parentTransform);

                    // Initializare agent cu parametrii selectati de utilizator
                    switch (i)
                    {
                        // Helios
                        case 0:
                            // Instantiaza agentul cu parametri selectati de utilizator
                            currentPlaceableObject.gameObject.GetComponent<TerestrialHeliosAgent>().Initialize(
                                // Deplasare
                                GameManager.Instance.HeliosParameters.MoveSpeed,
                                GameManager.Instance.HeliosParameters.RotationSpeed,
                                GameManager.Instance.HeliosParameters.SearchProximity,
                                // Infometare
                                GameManager.Instance.HeliosParameters.hungerFactor,
                                GameManager.Instance.HeliosParameters.hungerTickValue,
                                GameManager.Instance.HeliosParameters.timeBetweenHungerTicks
                                );

                            // Folosit pentru StatisticsManager (a contoriza numarul de agenti instantiati de fiecare tip)
                            agentInstantiated = "helios";

                            break;
                        // Mulak
                        case 1:
                            // Instantiaza agentul cu parametri selectati de utilizator
                            currentPlaceableObject.gameObject.GetComponent<TerestrialMulakAgent>().Initialize(
                                // Deplasare
                                GameManager.Instance.MulakParameters.MoveSpeed,
                                GameManager.Instance.MulakParameters.RotationSpeed,
                                GameManager.Instance.MulakParameters.SearchProximity,
                                // Imperechere
                                GameManager.Instance.MulakParameters.MateProximity,
                                // Infometare
                                GameManager.Instance.MulakParameters.hungerFactor,
                                GameManager.Instance.MulakParameters.hungerTickValue,
                                GameManager.Instance.MulakParameters.timeBetweenHungerTicks
                                );

                            // Folosit pentru StatisticsManager (a contoriza numarul de agenti instantiati de fiecare tip)
                            agentInstantiated = "mulak";

                            break;
                        // Galvadon
                        case 2:
                            // Instantiaza agentul cu parametri selectati de utilizator
                            currentPlaceableObject.gameObject.GetComponent<TerestrialGalvadonAgent>().Initialize(
                                // Deplasare
                                GameManager.Instance.GalvadonParameters.MoveSpeed,
                                GameManager.Instance.GalvadonParameters.RotationSpeed,
                                GameManager.Instance.GalvadonParameters.SearchProximity,
                                // Infometare
                                GameManager.Instance.GalvadonParameters.hungerFactor,
                                GameManager.Instance.GalvadonParameters.hungerTickValue,
                                GameManager.Instance.GalvadonParameters.timeBetweenHungerTicks
                                );

                            // Folosit pentru StatisticsManager (a contoriza numarul de agenti instantiati de fiecare tip)
                            agentInstantiated = "galvadon";

                            break;
                        // Phaoris
                        case 3:
                            // Instantiaza agentul cu parametri selectati de utilizator
                            currentPlaceableObject.gameObject.GetComponent<AerialPhaorisAgent>().Initialize(
                                GameManager.Instance.PhaorisParameters.MoveSpeed,
                                GameManager.Instance.PhaorisParameters.Y_RotationSpeed,
                                GameManager.Instance.PhaorisParameters.X_RotationSpeed,
                                GameManager.Instance.PhaorisParameters.SearchProximity,
                                GameManager.Instance.PhaorisParameters.DeliveryDistance
                                );

                            // Folosit pentru StatisticsManager (a contoriza numarul de agenti instantiati de fiecare tip)
                            agentInstantiated = "phaoris";

                            break;

                           
                    }

                    
                }
            }  
            else if (Input.GetMouseButtonDown(1) || CanPlaceAgents == false)
                Destroy(currentPlaceableObject);
        }
    }
    

    // Metoda care muta agentul dupa mouse
    void MovePlaceableObjectToMouse()
    {
        Ray mousePositionRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hitInformation;

        if (Physics.Raycast(mousePositionRay, out hitInformation))
        {
            currentPlaceableObject.transform.position = hitInformation.point;
            currentPlaceableObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInformation.normal);
        }
    }

    // Metoda care roteste agentul utilizant rotita mouse-ului
    void RotateTroughMouseWheel()
    {
        mouseWheelRotation += Input.mouseScrollDelta.y;
        currentPlaceableObject.transform.Rotate(Vector3.up, mouseWheelRotation * mouseWheelRotationSpeed);
    }

    // Metoda care plaseaza agentul 
    void OnClickRelease()
    {
        if (Input.GetMouseButton(0) && CanPlaceAgents == true)
        {
            currentPlaceableObject = null;
            switch (agentInstantiated)
            {
                case "helios":
                    StatisticsManager.Instance.ModifyAgentsNumber("add", "Helios");
                    break;

                case "mulak":
                    StatisticsManager.Instance.ModifyAgentsNumber("add", "Mulak");
                    break;

                case "galvadon":
                    StatisticsManager.Instance.ModifyAgentsNumber("add", "Galvadon");
                    break;

                case "phaoris":
                    StatisticsManager.Instance.ModifyAgentsNumber("add", "Phaoris");
                    break;
            }

        }
    }
}
