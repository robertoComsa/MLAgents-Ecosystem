using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

[System.Serializable]
public class TerestrialSearchAgent : Agent
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] protected float moveSpeed = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] protected float rotationSpeed = 0f;
    [Tooltip("Distanta in care agentul cauta tinte")] [SerializeField] protected float searchProximity = 0f;

    //  ---------------------------------------------------------- VARIABILE ---------------------------------------------------- //

    // Pozitia de start (folosita in reasezarea agentului in scena)
    protected Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    protected Rigidbody rb;

    // Observatii legate de cea mai apropriata tinta
    protected Vector3 closestTargetPosition = Vector3.zero;
    protected float distanceToClosestTarget = 0f;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    protected float timeGap = 0f;
    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    protected Vector3 targetedRayPos = Vector3.zero;

    // Tag-ul tintei
    protected string tagName = "";

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la searchProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        Vector3 toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        AddVectorObs(toClosestTarget.normalized); // 1 Vector3 = 3 valori float

        // Total: 10 + Observatiile de tip raycast
    }

    /// <summary>
    /// Alege actiuni pe baza unui vector ( de valori discrete )
    /// Index 0: Decide daca agentul se misca (valori disrecte; 0 - sta pe loc , 1 inainteaza misca )
    /// Index 1: Decide daca agentul se roteste (0 -> Nu se roteste; 1 -> se roteste la stanga; 2 -> se roteste la dreapta)
    /// </summary>
    /// <param name="vectorAction"> Vector de valori pe care reteaua neuronala le ofera pentru a lua anumite actiuni </param>
    public override void AgentAction(float[] vectorAction)
    {
        // Prima actiune 
        float forwardAmount = vectorAction[0];

        // A doua actiune
        float turnAmount = 0f; // -> Nu se roteste 
        if (vectorAction[1] == 1f)
        {
            turnAmount = -1f;  // -> Rotire stanga
        }
        else if (vectorAction[1] == 2f)
        {
            turnAmount = 1f; // -> Rotire dreapta
        }

        // Aplica miscarea asupra agentului cu valorile alese 
        rb.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * turnAmount * rotationSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Cand tipul de comportament (behaviour type) este setat pe Heuristic , aceasta metoda este folosita
    /// Controleaza agentul (agentii) prin input de la un utilizator uman
    /// </summary>
    /// <returns> Returneaza vectorul de valori vectorAction (format de data aceasta de input uman, nu de reteaua neuronala ) </returns>
    public override float[] Heuristic()
    {
        // Seteaza datele ( 0 - sta pe loc ; 1 - se misca in fata ; 2 - se misca in spate) pentru primul vector de actiuni 
        float forwardAction = 0f;

        if (Input.GetKey(KeyCode.W))
            forwardAction = 1f;
        else if (Input.GetKey(KeyCode.S)) // Pentru mine am lasat si mers in spate pe s , nu vreau sa implementez asta la agent pentru ca poate 
            forwardAction = -1f;         // duce la un comportament nerealist

        // Seteaza datele ( 0 - nu se roteste ; 1 - se roteste la stanga ; 2 - se roteste la dreapta) pentru al doilea vector de actiuni 
        float turnAction = 0f;

        if (Input.GetKey(KeyCode.A))
            turnAction = 1f;
        else if (Input.GetKey(KeyCode.D))
            turnAction = 2f;

        // Put the actions into an array and return
        return new float[] { forwardAction, turnAction };
    }

    // Reseteaza agentul (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        PlaceRandomly(6f);
    }

    // -------------------------------------------------------- METODE ------------------------------------------------------- //

    // Apelata o singura data inainte de start.
    protected virtual void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    protected virtual void FixedUpdate()
    {
        // Inainte CheckPartnerInProximity ar fi fost apelata aici de 50 de ori pe secunda
        // Acum este apelata de 10 ori . 
        OptimizedCheckInRadius(Color.red);
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected virtual void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - timeGap >= 0.1f)
        {
            CheckTargetInProximity();
            timeGap = Time.time;
        }

        if (targetedRayPos != Vector3.zero)
            Debug.DrawLine(transform.position, targetedRayPos, rayColor);
    }

    /// <summary>
    /// Verifica daca exista tinta intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    protected virtual void CheckTargetInProximity()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(tagName);
        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        foreach (GameObject target in targets)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance < nearestDistance && distance < searchProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
            {
                targetInRadius = true;
                nearestDistance = distance;
                closestTarget = target;
            }
        }

        if (targetInRadius)
        {
            closestTargetPosition = closestTarget.transform.localPosition;
            distanceToClosestTarget = nearestDistance;
            targetedRayPos = closestTarget.transform.position;
        }
        else
        {
            closestTargetPosition = Vector3.zero;
            distanceToClosestTarget = 0f;
            targetedRayPos = Vector3.zero;
        }
    }

    // Redenumire a tagului pentru tinta agentului
    protected virtual void RenameTag(string newTagName)
    {
        tagName = newTagName;
    }

    // Plaseaza agentul aleatoriu in spatiul de antrenare. (Atat pozitie cat si rotatie)
    protected void PlaceRandomly(float value)
    {
        transform.position = new Vector3(startingPosition.x + Random.Range(-value, value), startingPosition.y, startingPosition.z + Random.Range(-value, value));
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        transform.rotation = newRotation;
    }
}
