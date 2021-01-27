using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class DashingMulakAgent : Agent
{
    // <>--<> VARIABILE VIZIBILE IN EDITOR <>--<>

    [Header("Parametri")]
    [Tooltip("Forta dash-ului")] [SerializeField] float dashForce = 0f;
    [Tooltip("Dash Cooldown")] [SerializeField] float dashCooldown = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] float rotationSpeed = 0f;
    [Tooltip("Raza in care cautam cea mai apopriata tinta")] [SerializeField] float searchProximity = 0f;
    [Tooltip("Mate Proximity")] [SerializeField] float mateProximity = 0f;


    [Header("Parametri infometare")]
    [Tooltip("Daca folosim sau nu infometarea")] [SerializeField] protected bool useStarving = false;
    [Tooltip("O data la cat timp scade factorul de infometare (secunde)")] [SerializeField] protected float starvingInterval = 0f;

    [Header("Parametri imperechere")]
    [Tooltip("Culoarea agentilor neimperecheati")] [SerializeField] protected Material notMatedColor = null;
    [Tooltip("Culoarea agentilor imperecheati")] [SerializeField] protected Material MatedColor = null;
    [Tooltip("Prefab mulak")] [SerializeField] protected GameObject mulakPrefab = null;

    // <>--<> VARIABILE DASH <>--<>

    // Dash direction
    Vector3 dashDirection = Vector3.zero;
    // Folosit pentru a stabili daca dash-ul este in cd 
    float timeGap = 0;
    // Boolean ce ne spune daca agentul poate aplica un dash
    bool isDashAllowed = true;

    // <>--<> VARIABILE <>--<>

    // Componenta fizica a agentului
    Rigidbody rb = null;
    // Pozitia initiala a agentului
    Vector3 startingPosition = Vector3.zero;

    // <>--<> VARIABILE LEGATE DE CEA MAI APROPRIATA TINTA <>--<>

    // Observatii legate de cea mai apropriata tinta
    Vector3 closestTargetPosition = Vector3.zero;
    float distanceToClosestTarget = 0f;
    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    float proximitySearchTimeGap = 0f;
    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    Vector3 targetedRayPos = Vector3.zero;
    // Directia in care se afla cea mai apropriata tinta
    Vector3 toClosestTarget = Vector3.zero;
    // Culoarea agentului
    protected Renderer agentColor;


    // Verifica daca agentul s-a imperecheat deja
    protected bool isMated = false;
    // accesor al variabilei isMated 
    public bool GetIsMated() { return isMated; }
    // Cate secunde dureaza pana un agent se poate imperechea din nou
    protected float secondsToResetMating = 0f;
    // Partenerul compatibil
    protected DashingMulakAgent compatiblePartner = null;


    // Factorul de infometare initial 
    float initialStarvingInterval = 0f;
    float hungerTimeGap = 0f;
    // boolean ce verifica daca a inceput simularea 
    protected bool simStarted = false;
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

        agentColor = GetComponent<Renderer>();

        // Disabling collision for placement purposes 
        rb.isKinematic = true;

        // Infometare
        initialStarvingInterval = starvingInterval;
        hungerTimeGap = 1f;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        // Observații de cautare / miscare

        AddVectorObs(rb.velocity.normalized); // 1 Vector3 = 3 float
        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 float

        AddVectorObs(closestTargetPosition.normalized); // 1 Vector3 = 3 float
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 Vector3 = 3 float

        toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        // Un produs dot intre directia in care se uita agentul si directia in care se afla cea mai apropriata tinta
        AddVectorObs(Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized)); // 1 valoare float

        AddVectorObs(toClosestTarget.normalized); // 1 Vector3 = 3 float
        AddVectorObs(transform.forward.normalized); // 1 Vector3 = 3 float

        // TOTAL_1:  17
    }

    /// <summary>
    /// Alege actiuni pe baza unui vector ( de valori discrete )
    /// Index 0: Decide directia in care agentul se indreapta (0 -> inainte , 1 -> la stanga , 2 -> la dreapta)
    /// Index 1: Decide daca agentul se roteste (0 -> Nu se roteste; 1 -> se roteste la stanga; 2 -> se roteste la dreapta)
    /// </summary>
    /// <param name="vectorAction"> Vector de valori pe care reteaua neuronala le ofera pentru a lua anumite actiuni </param>
    public override void AgentAction(float[] vectorAction)
    {
        // Prima actiune 
        float dashDirectionIndex = vectorAction[0];

        switch (dashDirectionIndex)
        {

            // Inainte
            case 0f:
                dashDirection = gameObject.transform.forward;
                break;

            // Stanga
            case 1f:
                dashDirection = -gameObject.transform.right;
                break;

            // Dreapta
            case 2f:
                dashDirection = gameObject.transform.right;
                break;
        }


        // A doua actiune
        float turnAmount = 0f; // -> Nu se roteste 

        if (vectorAction[1] == 1f)
            turnAmount = -1f;  // -> Rotire stanga

        else if (vectorAction[1] == 2f)
            turnAmount = 1f; // -> Rotire dreapta

        // Aplica dash-ul asupra agentului
        if (isDashAllowed)
        {
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
            isDashAllowed = false;
        }


        // Aplica a doua actiune (Rotire)
        transform.Rotate(transform.up * turnAmount * rotationSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Cand tipul de comportament (behaviour type) este setat pe Heuristic , aceasta metoda este folosita
    /// Controleaza agentul (agentii) prin input de la un utilizator uman
    /// </summary>
    /// <returns> Returneaza vectorul de valori vectorAction (format de data aceasta de input uman, nu de reteaua neuronala ) </returns>
    public override float[] Heuristic()
    {
        float dashDirectionIndex = 0f;

        if (Input.GetKey(KeyCode.A))
            dashDirectionIndex = 1f;
        else if (Input.GetKey(KeyCode.D))
            dashDirectionIndex = 2f;

        // Seteaza datele ( 0 - nu se roteste ; 1 - se roteste la stanga ; 2 - se roteste la dreapta) pentru al doilea vector de actiuni 
        float turnAction = 0f;

        if (Input.GetKey(KeyCode.Q))
            turnAction = 1f;
        else if (Input.GetKey(KeyCode.E))
            turnAction = 2f;

        // Put the actions into an array and return
        return new float[] { dashDirectionIndex, turnAction };
    }

    // -------------------------------------------------------- METODE DASHING SEARCH AGENT ------------------------------------------------------- //

    // Metoda de initializare a agentilor cu parametri alesi de utilizator/
    public virtual void Initialize(float df, float dc, float rs, float sp, float mp, float si)
    {
        // Deplasare
        dashForce = df;
        dashCooldown = dc;
        rotationSpeed = rs;
        searchProximity = sp;

        // Imperechere
        mateProximity = mp;

        // Infometare
        starvingInterval = si;
    }

    // Permite aplicarea unui dash o data la value secunde
    private void AllowDash(float value)
    {
        if (Time.time - timeGap >= value)
        {
            isDashAllowed = true;
            timeGap = Time.time;
        }
    }

    protected virtual void Update()
    {
        // Daca simularea s-a incheiat distrugem acest agent
        if (GameManager.Instance.SimulationEnded)
            Destroy(gameObject);

        if (simStarted == true)
            AllowDash(dashCooldown);

    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    protected virtual void FixedUpdate()
    {
        // Inainte CheckPartnerInProximity ar fi fost apelata aici de 50 de ori pe secunda
        // Acum este apelata de 10 ori . 
        OptimizedCheckInRadius(Color.green);

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

        if (GameManager.Instance.GetRaysEnabled() == true && isMated != true)
            DrawLine(transform.position, targetedRayPos, rayColor);
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

    private void CheckTargetInProximity()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("prey");
        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        if (!isMated)
        {
            foreach (GameObject target in targets)
            {
                if (target.gameObject != gameObject && target.GetComponent<DashingMulakAgent>().GetIsMated() == false)
                {
                    float distance = Vector3.Distance(transform.position, target.transform.position);

                    if (distance < nearestDistance && distance < searchProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
                    {
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = target;

                        compatiblePartner = target.GetComponent<DashingMulakAgent>();
                    }
                }
            }

            if (targetInRadius)
            {
                closestTargetPosition = closestTarget.transform.localPosition;
                distanceToClosestTarget = nearestDistance;
                targetedRayPos = closestTarget.transform.position;

                if (distanceToClosestTarget <= mateProximity)
                {
                   if(compatiblePartner != null && compatiblePartner.GetIsMated() == false && simStarted==true)
                    {
                        compatiblePartner.GetMated();
                        GetMated();
                    }
                }
            }

            else
            {
                // Give random pos through a function that checksif 10s passed and then gives a new random target positioN
                RandomTargetPositionGenerator();
            }
        }
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
        if (Time.time - hungerTimeGap >= 1f && useStarving == true && simStarted == true) // O data la timeBetweenHungerTicks secunde
        {
            // Verificam daca nu este pauza pusa
            if (GameManager.Instance.gamePaused == false)
            {
                hungerTimeGap = Time.time;
                starvingInterval -= 1;
            }
        }

        if (starvingInterval <= 0f)
        {
            // Distrugem acest agent (moare de foame)
            Destroy(gameObject);

            // Modificam datele simularii
            StatisticsManager.Instance.ModifySimData("mulakStarved");
            StatisticsManager.Instance.ModifyAgentsNumber("remove", "Mulak");
        }
    }

    // Dupa ce mananca agentul se satura (revine la valoarea maxima a factorului de infometare)
    protected void Eat() { starvingInterval = initialStarvingInterval; }

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

    //
    //
    //
    // <>--<> COLIZIUNI  <>--<>
    //
    //
    //

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("predator"))
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica daca a intrat intr-o coliziune ; daca da interzice amplasarea
        CheckIfAgentIsPlaceable(false, other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Verifica daca agentul este intr-o coliziune ; daca da interzice amplasarea
        CheckIfAgentIsPlaceable(false, other);
    }

    // Folosit la amplasarea agentilor
    private void OnTriggerExit(Collider other)
    {
        // Verifica daca agentul a iesit din coliziuni ( OnTriggerStay nu va permite amplasarea pana cand nu se parasesc toate coliziunile)
        CheckIfAgentIsPlaceable(true, other);
    }

    //
    //
    //
    // <>--<> METODE IMPERECHERE  <>--<>
    //
    //
    //

    // Metoda apelata cand un alt agent ia actiunea de imperechere asupra acestui agent.
    protected virtual void GetMated()
    {
        isMated = true;

        secondsToResetMating = Random.Range(7f, 11f);
        StartCoroutine(ResetMated());
        StartCoroutine(GiveBirth());
    }

    // Metoda care asteapta un anumit interval de timp inainte de a reseta posibilitatea de imperechere a agentului
    IEnumerator ResetMated()
    {
        agentColor.material = MatedColor;

        yield return new WaitForSeconds(secondsToResetMating);

        agentColor.material = notMatedColor;
        isMated = false;

    }

    // Metoda care asteapta un anumit interval de timp inainte de a da nastere / a se multiplica
    IEnumerator GiveBirth()
    {
        yield return new WaitForSeconds(secondsToResetMating - 4f); // Vrem sa multiplicam agentul inainte ca acesta sa fie gata de imperechere

        // Multiplicare
        if (StatisticsManager.Instance.GetMulakAgentsNumber() < GameManager.Instance.GetMulakMaxAgentsNumberValue())
        {
            Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
            GameObject mulakChild = Instantiate(mulakPrefab, gameObject.transform.position - new Vector3(0f, 0f, -1.4f), newRotation, gameObject.transform.parent.transform);
            mulakChild.GetComponent<DashingMulakAgent>().BirthInitialize();

            // Modificam datele simularii
            StatisticsManager.Instance.ModifySimData("mulaksCreated");
            StatisticsManager.Instance.ModifySimData("MulakAgentsNumber");
        }
    }

    // Initializare pentru agentii instantiati prin multiplicare
    public void BirthInitialize()
    {
        isMated = false;
        agentColor.material = notMatedColor;
    }
}
