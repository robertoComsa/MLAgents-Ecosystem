using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

[System.Serializable]
public class TerestrialSearchAgent : Agent
{
    // <>--<> VARIABILE VIZIBILE IN EDITOR <>--<>
[Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] protected float moveSpeed = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] protected float rotationSpeed = 0f;
    [Tooltip("Distanta in care agentul cauta tinte")] [SerializeField] protected float searchProximity = 0f;
    

    [Header("Parametrii infometare")]
    [Tooltip("Daca folosim sau nu infometarea")] [SerializeField] protected bool useStarving = false;
    [Tooltip("O data la cat timp scade factorul de infometare (secunde)")] [SerializeField] protected float timeBetweenHungerTicks = 0f;
    [Tooltip("Valoarea cu care scade factorul de infometare")] [SerializeField] protected float hungerTickValue = 0f;
    [Tooltip("Factorul de infometare")] [SerializeField] protected float hungerFactor = 0f;

    // <>--<> VARIABILE <>--<>

    // Pozitia de start (folosita in reasezarea agentului in scena)
    protected Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    protected Rigidbody rb;

    // Observatii legate de cea mai apropriata tinta
    protected Vector3 closestTargetPosition = Vector3.zero;
    protected float distanceToClosestTarget = 0f;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    protected float proximitySearchTimeGap = 0f;
    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    protected Vector3 targetedRayPos = Vector3.zero;

    // Tag-ul tintei
    protected string tagName = "";

    // Directia spre cea mai apropriata tinta
    protected Vector3 toClosestTarget = Vector3.zero;

    // Factorul de infometare initial 
    protected float initialHungerFactor = 0f;

    // boolean ce verifica daca a inceput simularea 
    protected bool simStarted = false;

    // Folosit pentru a infometa agentul
    protected float hungerTimeGap = 0f;

    //
    protected float randomTargetTimeGap = 0f;
    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;

        // Disabling collision for placement purposes 
        rb.isKinematic = true;
      
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la searchProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
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

    // Cod aplicat la inceputul unui episod (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        PlaceRandomly(30f);
    }

    // -------------------------------------------------------- METODE TERESTRIAL SEARCH AGENT ------------------------------------------------------- //

    protected virtual void Update()
    {
        // Daca simularea s-a incheiat distrugem acest agent
        if (GameManager.Instance.SimulationEnded)
            Destroy(gameObject);
    }

    // Apelata o singura data inainte de start.
    protected virtual void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // Metoda de initializare a agentilor cu parametri alesi de utilizator/
    public virtual void Initialize(int ms, int rs, int sp , float hF , float hTv , float tBHT)
    {
        // Deplasare
        moveSpeed = ms;
        rotationSpeed = rs;
        searchProximity = sp;

        // Infometare
        hungerFactor = hF;
        hungerTickValue = hTv;
        timeBetweenHungerTicks = tBHT;

        // Initializare a factorului de infomatare intial
        initialHungerFactor = hungerFactor;
        hungerFactor += hungerTickValue;
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    protected virtual void FixedUpdate()
    {
        // Inainte CheckPartnerInProximity ar fi fost apelata aici de 50 de ori pe secunda
        // Acum este apelata de 10 ori . 
        OptimizedCheckInRadius(Color.red);

        // Permitem agentului sa ia decizii 
       if (GameManager.Instance.CanAgentsRequestDecisions == true)
        {
            RequestDecision();
            if (simStarted == false)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                simStarted = true;
            }
        }

        // Proces infometare
        if (useStarving) StarvingProcess();
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected virtual void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - proximitySearchTimeGap >= 0.1f)
        {
            CheckTargetInProximity();
            proximitySearchTimeGap = Time.time;
        }

        if (GameManager.Instance.GetRaysEnabled()==true)
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

            if (distance < nearestDistance && distance < searchProximity)
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
            // Give random pos through a function that checksif 10s passed and then gives a new random target positioN
            RandomTargetPositionGenerator();
        }
    }

    // O data la 10s daca agentul nu are o tinta acesta primeste aleatoriu un loc din spatiul de antrenare ca tinta
    protected virtual void RandomTargetPositionGenerator()
    {
        if (Time.time - randomTargetTimeGap >= 10f)
        {
            randomTargetTimeGap = Time.time;

            closestTargetPosition = new Vector3(Random.Range(-180f, 180f), transform.position.y, Random.Range(-180f, 180f));
            distanceToClosestTarget = Vector3.Distance(transform.position, closestTargetPosition);
            targetedRayPos = closestTargetPosition;
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

    //
    //
    //
    // <>--<> METODE FOLOSITE IN SISTEMUL DE PLASARE AL AGENTILOR IN SCENA DE CATRE UN UTILIZATOR UMAN  <>--<>
    //
    //
    //

    // Functie de verificare a colliderului folosita la amplasarea agentilor
    protected bool CheckColliderTag(Collider other)
    {
        if (other.CompareTag("predator") || other.CompareTag("prey") || other.CompareTag("helper") || other.CompareTag("Untagged") || other.CompareTag("boundary"))
            return true;

        return false;
    }

    // Metoda care verifica daca suntem in modul de amplasare si permite/interzice amplasarea agentilor in functie de coliziuni cu obiecte
    protected void CheckIfAgentIsPlaceable(bool allowPlacement, Collider other)
    {
        if (GameManager.Instance.CanAgentsRequestDecisions == false && CheckColliderTag(other) == true) // Inseamna ca e in placing mode
            // permitem sau interzicem amplasarea
            PlacementController.Instance.CanPlaceAgents = allowPlacement;
    }

    //
    //
    //
    // <>--<> METODE FOLOSITE PENTRU PROCESUL DE INFOMETARE / HRANIRE  <>--<>
    //
    //
    //

    // Metoda ce infometeaza agentul o data cu trecerea timpului
    protected virtual void StarvingProcess()
    {
        if (Time.time - hungerTimeGap >= timeBetweenHungerTicks && useStarving == true && simStarted == true) // O data la timeBetweenHungerTicks secunde
        {
            // Verificam daca nu este pauza pusa
            if (GameManager.Instance.gamePaused == false)
            {
                hungerFactor -= hungerTickValue;
                //Debug.Log(hungerFactor);
                hungerTimeGap = Time.time;
            }
        }
    }

    // Dupa ce mananca agentul se satura (revine la valoarea maxima a factorului de infometare)
    protected void Eat() { hungerFactor = initialHungerFactor; }

    // Metoda de draw line
    protected void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.02f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

        lr.startColor = color;
        lr.endColor = color;


        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }
}
