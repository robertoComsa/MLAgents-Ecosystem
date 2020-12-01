using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerestrialMulakAgent : TerestrialSearchAgent
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametrii imperechere")]
    [Tooltip("Culoarea agentilor neimperecheati")] [SerializeField] protected Material notMatedColor = null;
    [Tooltip("Culoarea agentilor imperecheati")] [SerializeField] protected Material MatedColor = null;
    [Tooltip("Distanta necesara pentru imperechere")] [SerializeField] protected float mateProximity = 0f;

    [Header("Variabile folosite pentru reproducere")]
    [Tooltip("Prefab mulak")] [SerializeField] protected GameObject mulakPrefab = null;

    [Header("Prefab pentru floare (hrana Galvadon)")]
    [SerializeField] protected GameObject flowerPrefab = null;

    //  ---------------------------------------------------------- VARIABILE ---------------------------------------------------- //

    protected Renderer agentColor;
    // Verifica daca agentul este pe pamant
    protected bool isGrounded = true;
    // Verifica daca agentul s-a imperecheat deja
    protected bool isMated = false;
    // accesor al variabilei isMated 
    public bool GetIsMated() { return isMated; }
    // Cate secunde dureaza pana un agent se poate imperechea din nou
    protected float secondsToResetMating = 0f;
    // Partenerul compatibil
    protected TerestrialMulakAgent compatiblePartner = null;

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        agentColor = GetComponent<Renderer>();
    }

    /// <summary>
    /// Alege actiuni pe baza unui vector ( de valori discrete )
    /// Index 0: Decide daca agentul se misca (valori disrecte; 0 - sta pe loc , 1 inainteaza misca )
    /// Index 1: Decide daca agentul se roteste (0 -> Nu se roteste; 1 -> se roteste la stanga; 2 -> se roteste la dreapta)
    /// Index 2: Decide daca agentul apeleaza metoda de imperechere sau nu
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

        // A 3-a actiune
        float mateAction = 0f; // -> Nu se imperecheaza
        if (vectorAction[2] == 1f)
        {
            mateAction = 1f; // -> Se imperecheaza
        }

        // Aplicam actiunea de imperechere 
        if (mateAction != 0) Mate();

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

        float mateAction = 0f;
        if (Input.GetKeyDown(KeyCode.Space)) mateAction = 1f;

        // Put the actions into an array and return
        return new float[] { forwardAction, turnAction, mateAction };
    }

    // Reseteaza agentul (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        base.AgentReset();
        isMated = false;
        agentColor.material = notMatedColor;
        isGrounded = true;
    }

    // -------------------------------------------------------- METODE (Mostenite din) TERESTRIAL SEARCH AGENT ------------------------------------------- //

    // Redenumirea tintei
    protected override void Awake()
    {
        base.Awake();
        RenameTag("prey");
    }

    // Metoda de initializare a agentilor cu parametri alesi de utilizator
    public  void Initialize(int ms, int rs, int sp, int mp , float hF , float hTv , float tBHT)
    {
        base.Initialize(ms, rs, sp , hF , hTv , tBHT);
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
                if (target.gameObject != gameObject && target.GetComponent<TerestrialMulakAgent>().GetIsMated() == false)
                {
                    float distance = Vector3.Distance(transform.position, target.transform.position);

                    if (distance < nearestDistance && distance < searchProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
                    {
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = target;

                        compatiblePartner = target.GetComponent<TerestrialMulakAgent>();
                    }
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
                distanceToClosestTarget = searchProximity;
                targetedRayPos = Vector3.zero;
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

            // Reward pentru directia in care se uita agentul ( 1 - maxim cand se uita direct la tinta , -1 - minim cand se uita in directia opusa)
            AddReward(0.01f * Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized));

            // Reward pentru distanta fata de tinta.
            AddReward(-0.01f * distanceToClosestTarget / searchProximity);
        }

        if (targetedRayPos != Vector3.zero && isMated != true)
            Debug.DrawLine(transform.position, targetedRayPos, Color.yellow);
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    protected override void FixedUpdate()
    {
        // Cautam si alegem cea mai apropriata tinta din proximitatea aleasa
        OptimizedCheckInRadius(rayColor: Color.yellow);

        // Permitem agentului sa ia decizii 
        if (isGrounded == true && GameManager.Instance.CanAgentsRequestDecisions == true)
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
        StarvingProcess();
    }


    // La coliziunea cu alte obiecte din scena
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
            isGrounded = true;

        if (other.gameObject.CompareTag("predator"))
        {
            Destroy(gameObject);
            //SetReward(-1f);
            //Done();
        }

        if (other.gameObject.CompareTag("boundary"))
        {
            AddReward(-0.5f);
            //AgentReset();
        }

        if (other.gameObject.CompareTag("helper") && other.gameObject.GetComponent<TerestrialGalvadonAgent>().GetCarryingFood() == true)
        {
            StartCoroutine(MakeFlower());
            // Mananca (starving system)
            Eat();
        }

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


    // ---------------------------------------------------- METODE MULAK ------------------------------------------------ //

    // Metoda apelata de agent (sau de un utilizator uman). 
    // Reprezinta a 3-a actiune pe care o poate lua agentul si anume de a se imperechea cu un partener *Compatibil*
    protected virtual void Mate()
    {
        if (distanceToClosestTarget <= mateProximity && isGrounded && compatiblePartner != null && compatiblePartner.GetIsMated() == false)
        {
            compatiblePartner.GetMated();
            GetMated();
            AddReward(1f);
        }
        else AddReward(-0.1f);
        
    }

    // Metoda apelata cand un alt agent ia actiunea de imperechere asupra acestui agent.
    protected virtual void GetMated()
    {
        closestTargetPosition = Vector3.zero;
        isMated = true;
        rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
        isGrounded = false;
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
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        GameObject mulakChild = Instantiate(mulakPrefab, gameObject.transform.position - new Vector3(0f, 0f, -1.4f), newRotation, gameObject.transform.parent.transform);
        mulakChild.GetComponent<TerestrialMulakAgent>().BirthInitialize();
    }

    // !!!!! De adaugat un bool daca a fost hranit , si daca da sa lase o floare inainte de a fi mancat 

    IEnumerator MakeFlower()
    {
        float waitTime = Random.Range(2f, 4f);  // 3 si 6 initial
        yield return new WaitForSeconds(waitTime);
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        Instantiate(flowerPrefab, gameObject.transform.position - new Vector3(0f, 0f, -1.4f), newRotation, gameObject.transform.parent.transform);
    }

    // Initializare pentru agentii instantiati prin multiplicare
    public void BirthInitialize()
    {
        isMated = false;
        agentColor.material = notMatedColor;
    }
}
