
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class MulakAgentv1 : Agent
{
    // VARIABILE VIZIBILE IN EDITOR

    [Header("Parametrii miscare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] private float moveSpeed = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] private float rotationSpeed = 0f;

    [Header("Parametrii depistare partener")]
    [Tooltip("Distanta in care agentul isi cauta partener")] [SerializeField] private float partnerProximity = 0f;
    [Tooltip("Distanta in care agentul se poate imperechea")] [SerializeField] private float mateProximity = 0f;

    [Header("Parametrii imperechere")]
    [Tooltip("Culoarea agentilor neimperecheati")] [SerializeField] private Material notMatedColor = null;
    [Tooltip("Culoarea agentilor imperecheati")] [SerializeField] private Material MatedColor = null;

    [Header("Variabile folosite pentru reproducere")]
    [Tooltip("Prefab mulak")] [SerializeField] private GameObject mulakPrefab = null;
    [Tooltip("Transformul parintelui")] [SerializeField] private Transform trainingArea = null;

    [Header("Prefab pentru floare (hrana Galvadon)")]
    [SerializeField] private GameObject flowerPrefab = null;

    // VARIABILE

    // Pozitia de start (folosita in reasezarea agentului in scena)
    private Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    private Rigidbody rb;
    // Componenta material a agentului
    private Renderer agentColor;
    // Verifica daca agentul este pe pamant
    private bool isGrounded = true;
    // Verifica daca agentul s-a imperecheat deja
    private bool isMated = false;
    // accesor al variabilei isMated 
    public bool GetIsMated() { return isMated; }
    // Cate secunde dureaza pana un agent se poate imperechea din nou
    private float secondsToResetMating = 0f;

    // Observatii legate de cel mai apropriat partener
    private Vector3 closestPartnerPosition = Vector3.zero;
    private float distanceToClosestPartner = Mathf.Infinity;

    // Partenerul compatibil
    private MulakAgentv1 compatiblePartner = null;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    private float timeGap = 0f;
    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    private Vector3 targetedRayPos = Vector3.zero;

    // METODE (Mostenite din) AGENT

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rb = GetComponent<Rigidbody>();
        agentColor = GetComponent<Renderer>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
    }

    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestPartner / partnerProximity); // 1 valoare float;

        Vector3 toClosestPartner = closestPartnerPosition - gameObject.transform.localPosition;
        AddVectorObs(toClosestPartner.normalized); // 1 Vector3 = 3 valori float

        // Total: 10 + Observatiile de tip raycast
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
        if (mateAction!=0) Mate();

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
        return new float[] { forwardAction, turnAction ,mateAction};
    }

    // Reseteaza agentul si spatiul de antrenare 
    public override void AgentReset()
    {
        PlaceRandomly(6f);
        isMated = false;
        agentColor.material = notMatedColor;
    }

    // ----------------------------------------------- METODE --------------------------------------------

    // Apelata o singura data inainte de start.
    private void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    /// <summary>
    /// Verifica daca exista parteneri (compatibili) intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    public void CheckPartnerInProximity()
    {
        GameObject[] partners = GameObject.FindGameObjectsWithTag("prey");
        float nearestDistance = Mathf.Infinity;
        GameObject closestPartner = null;
        bool partnerInRadius = false;

        if (!isMated)
        {
            foreach (GameObject partner in partners)
            {
                if (partner.gameObject != gameObject && partner.GetComponent<MulakAgentv1>().GetIsMated() == false)
                {
                    float distance = Vector3.Distance(transform.position, partner.transform.position);

                    if (distance < nearestDistance && distance < partnerProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
                    {
                        partnerInRadius = true;
                        nearestDistance = distance;
                        closestPartner = partner;

                        compatiblePartner = partner.GetComponent<MulakAgentv1>();
                    }
                }
            }

            if (partnerInRadius)
            {
                closestPartnerPosition = closestPartner.transform.localPosition;
                distanceToClosestPartner = nearestDistance;
                targetedRayPos = closestPartner.transform.position;
            }

            else
            {
                closestPartnerPosition = Vector3.zero;
                distanceToClosestPartner = 0f;
                targetedRayPos = Vector3.zero;
            }
        }
    }

    // Plaseaza agentul aleatoriu in spatiul de antrenare. (Atat pozitie cat si rotatie)
    // Folosit in AgentReset()
    public void PlaceRandomly(float value)
    {
        transform.position = new Vector3(startingPosition.x + Random.Range(-value, value), startingPosition.y, startingPosition.z + Random.Range(-value, value));
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        transform.rotation = newRotation;
    }

    // Metoda ce detecteaza coliziunea cu limitele spatiului de antrenare
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boundary"))
        {
            SetReward(-1f);
            Done();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
        if(other.gameObject.CompareTag("predator"))
        {
            SetReward(-1f);
            Destroy(gameObject);
            //Done(); - Folosit doar in Training
        }
        if(other.gameObject.CompareTag("helper") && other.gameObject.GetComponent<Galvadon2Final>().GetCarryingFood() == true)
        {
            StartCoroutine(MakeFlower());
        }
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    private void OptimizedCheckInRadius()
    {
        if (Time.time - timeGap >= 0.1f)
        {
            CheckPartnerInProximity();
            timeGap = Time.time;
        }

        if (targetedRayPos != Vector3.zero && isMated != true)
            Debug.DrawLine(transform.position, targetedRayPos, Color.yellow);
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    private void FixedUpdate()
    {
        // Inainte CheckPartnerInProximity ar fi fost apelata aici de 50 de ori pe secunda
        // Acum este apelata de 10 ori . 
        OptimizedCheckInRadius();

        if (distanceToClosestPartner != 0)
            AddReward(-(distanceToClosestPartner / partnerProximity) / maxStep);
        else AddReward(-1f / maxStep); // AddReward(-1f / maxStep); // x 50 de apeluri -> -0.01s pe secunda

        if (isGrounded) RequestDecision(); // Nu vrem ca agentul sa ia decizii cand este in aer 
    }

    // Metoda apelata de agent (sau de un utilizator uman). 
    // Reprezinta a 3-a actiune pe care o poate lua agentul si anume de a se imperechea cu un partener *Compatibil*
    public void Mate()
    {
        if (distanceToClosestPartner <= mateProximity && isGrounded && compatiblePartner!=null && compatiblePartner.GetIsMated() == false)
        {
            compatiblePartner.GetMated();
            GetMated();
        }
        else AddReward(-0.1f);
    }

    // Metoda apelata cand un alt agent ia actiunea de imperechere asupra acestui agent.
    public void GetMated()
    {
        closestPartnerPosition = Vector3.zero;
        isMated = true;
        rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
        isGrounded = false;
        secondsToResetMating = Random.Range(10f, 14f);
        StartCoroutine(ResetMated());
        StartCoroutine(GiveBirth());
        AddReward(1f);
        SetReward(1f);
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
        yield return new WaitForSeconds(secondsToResetMating-4f); // Vrem sa multiplicam agentul inainte ca acesta sa fie gata de imperechere
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        GameObject mulakChild  = Instantiate(mulakPrefab , gameObject.transform.position - new Vector3(0f,0f,-1.4f), newRotation , trainingArea);
        mulakChild.GetComponent<MulakAgentv1>().BirthInitialize();
    }

    // !!!!! De adaugat un bool daca a fost hranit , si daca da sa lase o floare inainte de a fi mancat 

    IEnumerator MakeFlower()
    {
        float waitTime = Random.Range(2f, 4f);  // 3 si 6 initial
        yield return new WaitForSeconds(waitTime);
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        Instantiate(flowerPrefab, gameObject.transform.position - new Vector3(0f, 0f, -1.4f), newRotation, trainingArea);
    }

    // Initializare pentru agentii instantiati prin multiplicare
    public void BirthInitialize()
    {
        isMated = false;
        agentColor.material = notMatedColor;
    }

}
