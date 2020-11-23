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

    public bool CanPlaceAgents{ get; set; } = true;

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
                            currentPlaceableObject.gameObject.GetComponent<TerestrialHeliosAgent>().Initialize(
                                GameManager.Instance.HeliosParameters.MoveSpeed,
                                GameManager.Instance.HeliosParameters.RotationSpeed,
                                GameManager.Instance.HeliosParameters.SearchProximity
                                );
                            break;
                        // Mulak
                        case 1:
                            currentPlaceableObject.gameObject.GetComponent<TerestrialMulakAgent>().Initialize(
                                GameManager.Instance.MulakParameters.MoveSpeed,
                                GameManager.Instance.MulakParameters.RotationSpeed,
                                GameManager.Instance.MulakParameters.SearchProximity,
                                GameManager.Instance.MulakParameters.MateProximity
                                );
                            break;
                        // Galvadon
                        case 2:
                            currentPlaceableObject.gameObject.GetComponent<TerestrialGalvadonAgent>().Initialize(
                                GameManager.Instance.GalvadonParameters.MoveSpeed,
                                GameManager.Instance.GalvadonParameters.RotationSpeed,
                                GameManager.Instance.GalvadonParameters.SearchProximity
                                );
                            break;
                        // Phaoris
                        case 3:
                            currentPlaceableObject.gameObject.GetComponent<AerialPhaorisAgent>().Initialize(
                                GameManager.Instance.PhaorisParameters.MoveSpeed,
                                GameManager.Instance.PhaorisParameters.Y_RotationSpeed,
                                GameManager.Instance.PhaorisParameters.X_RotationSpeed,
                                GameManager.Instance.PhaorisParameters.SearchProximity,
                                GameManager.Instance.PhaorisParameters.DeliveryDistance
                                );
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
            currentPlaceableObject = null;
    }
}
