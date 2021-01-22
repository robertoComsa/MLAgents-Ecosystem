using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MulakSearching : TerestrialSearchAgent
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametri")]
    [Tooltip("Componenta RB a parintelui")] [SerializeField] private Rigidbody agentRB = null;
    [Tooltip("Agent collision logics")] [SerializeField] private MulakAgentCollisionLogics agentCollisionLogics = null;

    [Header("Parametrii imperechere")]
    [Tooltip("Culoarea agentilor neimperecheati")] [SerializeField] protected Material notMatedColor = null;
    [Tooltip("Culoarea agentilor imperecheati")] [SerializeField] protected Material MatedColor = null;
    [Tooltip("Distanta necesara pentru imperechere")] [SerializeField] protected float mateProximity = 0f;

    [Header("Variabile folosite pentru reproducere")]
    [Tooltip("Prefab mulak")] [SerializeField] protected GameObject mulakPrefab = null;

    // ---------------------------------------------------------- VARIABILE ------------------------------------------------------- //

    protected Renderer agentColor;
    // Verifica daca agentul s-a imperecheat deja
    protected bool isMated = false;
    // accesor al variabilei isMated 
    public bool GetIsMated() { return isMated; }
    // Cate secunde dureaza pana un agent se poate imperechea din nou
    protected float secondsToResetMating = 0f;
    // Partenerul compatibil
    protected MulakSearching compatiblePartner = null;

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        agentRB.centerOfMass = Vector3.zero;
        agentRB.inertiaTensorRotation = Quaternion.identity;

        // Disabling collision for placement purposes 
        agentRB.isKinematic = true;
        // Caching color
        agentColor = GetComponentInParent<Renderer>();
    }

    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la huntProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        Vector3 toClosestPrey = closestTargetPosition - gameObject.transform.localPosition;
        AddVectorObs(toClosestPrey.normalized); // 1 Vector3 = 3 valori float

        // Un produs dot intre directia in care se uita agentul si directia in care se afla cel mai apropriat erbivor
        AddVectorObs(Vector3.Dot(gameObject.transform.forward.normalized, toClosestPrey.normalized)); // 1 valoare float

        // Total: 11 + Observatiile de tip raycast
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
        agentRB.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
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

    // -------------------------------------------------------- METODE (Mostenite din) TERESTRIAL SEARCH AGENT ------------------------------------------- //

    protected override void Update()
    {}

    // Redenumirea tintei
    protected override void Awake()
    {
        base.Awake();
        RenameTag("prey");
    }

    // Metoda de initializare a agentilor cu parametri alesi de utilizator
    public void Initialize(int ms, int rs, int sp, int mp, float hF, float hTv, float tBHT)
    {
        base.Initialize(ms, rs, sp, hF, hTv, tBHT);
        mateProximity = mp;
    }

    /// <summary>
    /// Verifica daca exista tinta intr-un radius setat in jurul agentului (( SI )) daca nu este imperecheat deja.
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa , de asemenea salveaza-l 
    /// </summary>
    protected override void CheckTargetInProximity()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(tagName);
        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        if (!isMated)
        {
            foreach (GameObject target in targets)
            {
                if (target.gameObject != gameObject && target.GetComponentInChildren<MulakSearching>().GetIsMated() == false)
                {
                    float distance = Vector3.Distance(transform.position, target.transform.position);

                    if (distance < nearestDistance && distance < searchProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
                    {
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = target;

                        compatiblePartner = target.GetComponentInChildren<MulakSearching>();
                    }
                }
            }

            if (targetInRadius)
            {
                closestTargetPosition = closestTarget.transform.localPosition;
                distanceToClosestTarget = nearestDistance;
                targetedRayPos = closestTarget.transform.position;

                // Daca e in mate proximity aplica automat imperecherea
            }

            else
            {
                // Give random pos through a function that checksif 10s passed and then gives a new random target positioN
                RandomTargetPositionGenerator();
            }
        }
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected override void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - proximitySearchTimeGap >= 0.1f)
        {
            CheckTargetInProximity();
            proximitySearchTimeGap = Time.time;
        }

        if (GameManager.Instance.GetRaysEnabled() == true && isMated != true)
            DrawLine(transform.position, targetedRayPos, rayColor);
    }

    protected override void StarvingProcess()
    {
        base.StarvingProcess();

        if (hungerFactor <= 0f)
        {
            // Distrugem acest agent (moare de foame)
            Destroy(gameObject);

            // Modificam datele simularii
            StatisticsManager.Instance.ModifySimData("mulakStarved");
            StatisticsManager.Instance.ModifyAgentsNumber("remove", "Mulak");
        }
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    protected override void FixedUpdate()
    {
        // Cautam si alegem cea mai apropriata tinta din proximitatea aleasa
        OptimizedCheckInRadius(rayColor: Color.yellow);

        // Permitem agentului sa ia decizii 
        if (agentCollisionLogics.GetAgentGrounded() == true && GameManager.Instance.CanAgentsRequestDecisions == true)
        {
            RequestDecision();
            if (simStarted == false)
            {
                agentRB.isKinematic = false;
                agentRB.detectCollisions = true;
                simStarted = true;
            }
        }

        // Proces infometare
        if (useStarving) StarvingProcess();
    }

    // ---------------------------------------------------- METODE MULAK ------------------------------------------------ //

    protected virtual void Mate()
    {
        if (distanceToClosestTarget <= mateProximity  && compatiblePartner != null && compatiblePartner.GetIsMated() == false)
        {
            compatiblePartner.GetMated();
            GetMated();
        }
    }

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
            mulakChild.GetComponentInChildren<MulakSearching>().BirthInitialize();

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
