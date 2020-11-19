using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PlacementController : MonoBehaviour
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

    // ------------------------------------------------------------- METODE ------------------------------------------------------- // 

    private void Awake()
    {
        agentsNumber = placeableAgentsPrefabs.Length;
    }

    private void Update()
    {
        HandleNewObjectHotkey();

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
                if(currentPlaceableObject == null)
                    currentPlaceableObject = Instantiate(placeableAgentsPrefabs[i], parentTransform);
            }  
            else if (Input.GetMouseButtonDown(1))
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
        if (Input.GetMouseButton(0))
            currentPlaceableObject = null;
    }
}
