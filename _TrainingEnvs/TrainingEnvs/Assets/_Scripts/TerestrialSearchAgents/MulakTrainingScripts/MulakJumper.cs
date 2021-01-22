using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class MulakJumper : Agent
{
    [Header("Parametri")]
    [Tooltip("Forta sariturii")] [SerializeField] protected float jumpForce = 0f;
    [Tooltip("Radiusul in care verificam daca sunt pradatori")] [SerializeField] protected float searchProximity = 0f;

    [Header("Parametrii imperechere")]
    [Tooltip("Culoarea agentilor neimperecheati")] [SerializeField] protected Material notMatedColor = null;
    [Tooltip("Culoarea agentilor imperecheati")] [SerializeField] protected Material MatedColor = null;
    [Tooltip("Distanta necesara pentru imperechere")] [SerializeField] protected float mateProximity = 0f;

    [Header("Variabile folosite pentru reproducere")]
    [Tooltip("Prefab mulak")] [SerializeField] protected GameObject mulakPrefab = null;

    // <>--<> VARIABILE <>--<>

    // Pozitia de start (folosita in reasezarea agentului in scena)
    Vector3 startingPosition = Vector3.zero;
    // Directia sariturii
    Vector3 jumpDirection = new Vector3(1f, 1.5f, -1f);
    // Timpul dintre sarituri
    private float timeGap = 0f;
    // boolean care ne spune daca agentul poate sari
    private bool jumpAllowed = false;
    // componenta rigidbody 
    private Rigidbody rb = null;

    // Observatii legate de cea mai apropriata tinta
    protected Vector3 closestTargetPosition = Vector3.zero;
    protected float distanceToClosestTarget = 0f;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    protected float proximitySearchTimeGap = 0f;
    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    protected Vector3 targetedRayPos = Vector3.zero;

    // Tag-ul tintei
    protected string tagName = "prey";

    // Directia spre cea mai apropriata tinta
    protected Vector3 toClosestTarget = Vector3.zero;

    // Factorul de infometare initial 
    protected float initialHungerFactor = 0f;

    // boolean ce verifica daca a inceput simularea 
    protected bool simStarted = false;

    // Folosit pentru a infometa agentul
    protected float hungerTimeGap = 0f;

    protected bool isMated = false;
    // accesor al variabilei isMated 
    public bool GetIsMated() { return isMated; }

    // Culoarea agentului
    protected Renderer agentColor;

    // Cate secunde dureaza pana un agent se poate imperechea din nou
    protected float secondsToResetMating = 0f;
    // Partenerul compatibil
    protected MulakJumper compatiblePartner = null;
    // Bool ce verifica daca agentul a fost hranit
    //bool wasFed = false;

    bool isGrounded = true;

    float randomTargetTimeGap = 0f;

    // ------------------------------------------------- METODE SISTEM ------------------------------------------------------ //

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agentColor = GetComponent<Renderer>();
    }

    private void FixedUpdate()
    {

        // Cautam si alegem cea mai apropriata tinta din proximitatea aleasa
        OptimizedCheckInRadius(rayColor: Color.yellow);

        // Permite sarituri o data la n secunde
        AllowJump(3f);

    }

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;

        // Disabling collision for placement purposes 
        rb.isKinematic = true;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(closestTargetPosition.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la searchProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        AddVectorObs(toClosestTarget.normalized); // 1 Vector3 = 3 valori float

        // Total: 10 + Observatiile de tip raycast
    }

    //
    public override void AgentAction(float[] vectorAction)
    {
        jumpDirection = new Vector3(Mathf.Clamp(vectorAction[0], -1, 1),
                                    Mathf.Clamp(vectorAction[1], 1, 2),
                                    Mathf.Clamp(vectorAction[2], -1, 1));

        if (isGrounded && jumpAllowed)
        {
            rb.AddForce(jumpDirection * Mathf.Clamp(vectorAction[3], 1, 4) * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            jumpAllowed = false;
        }
    }

    public override float[] Heuristic()
    {
        
        return new float[] { 0,0,0,0 };
    }

    // Cod aplicat la inceputul unui episod (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        gameObject.transform.position = startingPosition;

        rb.velocity = Vector3.zero;
        isMated = false;
        agentColor.material = notMatedColor;
        isGrounded = true;
    }

    // ---------------------------------------------------------- METODE MULAK ---------------------------------------------- //

    private float lastDistanceToClosestTarget = 0f;

    // Allow jumping once n seconds
    private void AllowJump(float value)
    {
        if (Time.time - timeGap >= value)
        {
            jumpAllowed = true;
            timeGap = Time.time;

            if(distanceToClosestTarget < lastDistanceToClosestTarget)
                AddReward(distanceToClosestTarget / searchProximity);

            lastDistanceToClosestTarget = distanceToClosestTarget;
        }
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected virtual void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - proximitySearchTimeGap >= 0.1f)
        {
            CheckTargetInProximity();
            proximitySearchTimeGap = Time.time;
        }

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

            // A ajuns in momentul de imperechere
            if(distanceToClosestTarget < mateProximity)
            {
                PlaceRandomly(10f);
                AddReward(1f); 
            }
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

    // Plaseaza agentul aleatoriu in spatiul de antrenare. (Atat pozitie cat si rotatie)
    protected void PlaceRandomly(float value)
    {
        transform.position = new Vector3(startingPosition.x + Random.Range(-value, value), startingPosition.y, startingPosition.z + Random.Range(-value, value));
    }
    // ---------------------------------------------------------- COLIZIUNI ------------------------------------------------- //

    // Cat timp atinge pamantul agentul "isGrounded"
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    // Cand agentul paraseste pamantul nu mai este grounded
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }

    // Cand agentul atinge un trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boundary"))
        {
            PlaceRandomly(10f);

            // Penalizare
            AddReward(-1f);
        }
    }
}
