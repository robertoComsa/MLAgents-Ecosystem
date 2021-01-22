using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class MulakDashingAgentScript: Agent
{
    // <>--<> VARIABILE VIZIBILE IN EDITOR <>--<>

    [Header("Parametri")]
    [Tooltip("Forta dash-ului")] [SerializeField] float dashForce = 0f;
    [Tooltip("Dash Cooldown")] [SerializeField] float dashCooldown = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] float rotationSpeed = 0f;
    [Tooltip("Raza in care verificam daca sunt pradatori")] [SerializeField] float safeRadius = 0f;
    [Tooltip("Raza in care cautam cea mai apopriata tinta")] [SerializeField] float searchProximity = 0f;
    [Tooltip("Mate Proximity")] [SerializeField] float mateProximity = 0f;

    [Header("Tinta")]
    [SerializeField] MulakDashingTarget target = null;

    [Header("Componenta parinte")]
    [SerializeField] Transform parentTransform = null;


    // <>--<> VARIABILE DASH <>--<>

    // Dash direction
    Vector3 dashDirection = Vector3.zero;
    // Folosit pentru a stabili daca dash-ul este in cd 
    float timeGap = 0;
    // Boolean ce ne spune daca agentul poate aplica un dash
    bool isDashAllowed = false;

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

    // <>--<> VARIABILE DEFENSIVE <>--<>

    // int folosit pe post de boolean ( 0 - nu avem pradator in radiusul de siguranta , 1 - avem)
    int predatorInsideRadius = 0;
    Vector3 predatorPosition = Vector3.zero;

    // -------------------------------------------------------- METODE SISTEM ------------------------------------------------ //

    private void Update()
    {
        // Permite aplicarea unei miscari o data la dashCooldown secunde
        AllowDash(dashCooldown);
    }

    private void FixedUpdate()
    {
        // Inainte CheckPartnerInProximity ar fi fost apelata aici de 50 de ori pe secunda
        // Acum este apelata de 10 ori . 
        OptimizedCheckInRadius(Color.green);
    }

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        // Initializare date componenta fizica
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;

        // Disabling collision for placement purposes 
        //rb.isKinematic = true;

        // Salvam pozitia initiala a agentului
        startingPosition = gameObject.transform.position;

        // Permitem aplicarea miscarilor
        isDashAllowed = true;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        // Observații de cautare / miscare

        AddVectorObs(rb.velocity.normalized); // 1 Vector3 = 3 float
        AddVectorObs(distanceToClosestTarget/searchProximity); // 1 float
        AddVectorObs(closestTargetPosition.normalized); // 1 Vector3 = 3 float
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 Vector3 = 3 float
        toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        // Un produs dot intre directia in care se uita agentul si directia in care se afla cea mai apropriata tinta
        AddVectorObs(Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized)); // 1 valoare float

        // TOTAL_1: 11 

        // Observatii defensive
        AddVectorObs(predatorInsideRadius); // 1 int

        // Sistemul de raze este conceput in principal pentru a depista pradatorii. Agentul primeste din acel sistem directia + distanta in care se afla pradatorul.

        // TOTAL_2: 1 
        
        // TOTAL_FINAL: 12
    }

    /// <summary>
    /// Alege actiuni pe baza unui vector ( de valori discrete )
    /// Index 0: Decide directia in care agentul se indreapta (0 -> Sta pe loc, 1 -> inainte , 2 -> inapoi , 3 -> la stanga , 4 -> la dreapta)
    /// Index 1: Decide daca agentul se roteste (0 -> Nu se roteste; 1 -> se roteste la stanga; 2 -> se roteste la dreapta)
    /// </summary>
    /// <param name="vectorAction"> Vector de valori pe care reteaua neuronala le ofera pentru a lua anumite actiuni </param>
    public override void AgentAction(float[] vectorAction)
    {
        // Prima actiune 
        float dashDirectionIndex = vectorAction[0];

        switch(dashDirectionIndex)
        {
            // Sta pe loc
            case 0f:
                dashDirection = Vector3.zero;
                break;

            // Inainte
            case 1f:
                dashDirection = gameObject.transform.forward;
                break;
            
            // Inapoi
            case 2f:
                dashDirection = -gameObject.transform.forward;
                break;

            // Stanga
            case 3f:
                dashDirection = -gameObject.transform.right;
                break;

            // Dreapta
            case 4f:
                dashDirection = gameObject.transform.right;
                break;
        }
            

        // A doua actiune
        float turnAmount = 0f; // -> Nu se roteste 

        if (vectorAction[1] == 1f)
            turnAmount = -1f;  // -> Rotire stanga

        else if (vectorAction[1] == 2f)
            turnAmount = 1f; // -> Rotire dreapta
     

        // A 3-a actiune
        float dashPowerMultiplier = 1f;

        if (vectorAction[2] == 1f)
            dashPowerMultiplier = 2f;

        else if (vectorAction[2] == 2f)
            dashPowerMultiplier = 3f;


        // Aplica dash-ul asupra agentului
        if(isDashAllowed)
        {
            rb.AddForce(dashDirection * dashForce * dashPowerMultiplier, ForceMode.Impulse);
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

        if (Input.GetKey(KeyCode.W))
            dashDirectionIndex = 1f;
        else if (Input.GetKey(KeyCode.S))
            dashDirectionIndex = 2f;
        else if (Input.GetKey(KeyCode.A))
            dashDirectionIndex = 3f;
        else if (Input.GetKey(KeyCode.D))
            dashDirectionIndex = 4f;

        // Seteaza datele ( 0 - nu se roteste ; 1 - se roteste la stanga ; 2 - se roteste la dreapta) pentru al doilea vector de actiuni 
        float turnAction = 0f;

        if (Input.GetKey(KeyCode.Q))
            turnAction = 1f;
        else if (Input.GetKey(KeyCode.E))
            turnAction = 2f;

        // Put the actions into an array and return
        return new float[] { dashDirectionIndex, turnAction ,2f};
    }

    // Cod aplicat la inceputul unui episod (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        // Resetarea pozitiei
        ResetPlacement();

        // Eliminam viteza aplicata componentei fizice
        rb.velocity = Vector3.zero;
    }

    // ------------------------------------------------------------------ METODE MULAK DASHING --------------------------------------------- // 

    // Permite aplicarea unui dash o data la value secunde
    private void AllowDash(float value)
    {
        if (Time.time - timeGap >= value)
        {
            isDashAllowed = true;
            timeGap = Time.time;
        }
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected virtual void OptimizedCheckInRadius(Color rayColor)
    {  
        if (Time.time - proximitySearchTimeGap >= 0.1f)
        {
            CheckTargetInProximity();
            CheckPredatorInRadius(safeRadius);
            proximitySearchTimeGap = Time.time;

            // Reward pentru directia in care se uita agentul ( 1 - maxim cand se uita direct la tinta , -1 - minim cand se uita in directia opusa)
            AddReward(0.01f * Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized));

            // Reward pentru distanta fata de tinta.
            AddReward(-0.01f * distanceToClosestTarget / searchProximity);
        }

        //if (GameManager.Instance.GetRaysEnabled() == true)
            DrawLine(transform.position, targetedRayPos, rayColor);
    }

    /// <summary>
    /// Verifica daca exista tinta intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    protected virtual void CheckTargetInProximity()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("training_target");
        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        foreach (GameObject target in targets)
        {
            if(target.gameObject != gameObject) // si daca nu e imperecheat
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);

                if (distance < nearestDistance && distance < searchProximity)
                {
                    targetInRadius = true;
                    nearestDistance = distance;
                    closestTarget = target;
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
                target.RandomTargetPositionGenerator();
                AddReward(0.5f);
                //Debug.Log(GetCumulativeReward());
            }
        }
        else
        {
            // Give random pos through a function that checksif 10s passed and then gives a new random target positioN
            RandomTargetPositionGenerator();
        }
    }

    // Metoda de verificare a pradatorilor intr-un radius
    private void CheckPredatorInRadius(float radius)
    {
        // Ne intereseaza daca cel putin 1 pradator este in radius
        Collider[] predatorCollider = new Collider[1];

        Physics.OverlapSphereNonAlloc(gameObject.transform.position, radius, predatorCollider, 9);

        if (predatorCollider[0] != null)
        {
            predatorInsideRadius = 1;
            predatorPosition = predatorCollider[0].gameObject.transform.localPosition;
        }
        else
        {
            predatorInsideRadius = 0;
            predatorPosition = gameObject.transform.localPosition;
        }

    }

    // 
    protected virtual void RandomTargetPositionGenerator()
    {
        closestTargetPosition = new Vector3(Random.Range(-90f,90f), 1f, Random.Range(-90f, 90f));
        distanceToClosestTarget = Vector3.Distance(transform.position, closestTargetPosition);
        targetedRayPos = closestTargetPosition;
    }

    // Folosita in antrenarea creierului defensiv mulak
    public void ResetPlacement()
    {
        gameObject.transform.position = new Vector3(parentTransform.localPosition.x + Random.Range(-45f, 45f),
                                                    1f,
                                                    parentTransform.localPosition.y + Random.Range(-45f, 45f));
    }

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


    // ------------------------------------------------------------------- COLIZIUNI ----------------------------------------------- //

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boundary"))
        {
            AgentReset();
            AddReward(-0.5f);
            //Debug.Log(GetCumulativeReward());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("predator"))
        {
            AgentReset();
            AddReward(-0.5f);
            //Debug.Log(GetCumulativeReward());
        }
    }
}
