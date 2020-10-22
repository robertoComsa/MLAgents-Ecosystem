using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PhaorisAgent : Agent
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] float moveSpeed = 1000f;
    [Tooltip("Viteza de giratie (rotire in jurul axei y)")] [SerializeField] float yRotSpeed = 100f;  // yaw
    [Tooltip("Viteza de inclinare (rotire in jurul axei z)")] [SerializeField] float xRotSpeed = 100f;  // pitch
    [Tooltip("Distanta de cautare")] [SerializeField] float searchProximity = 40f;
    [Tooltip("Proximitatea de livrare")] [SerializeField] float deliveryDistanceRequired = 5f;

    [Header("Ciocul pasarii")]
    [Tooltip("Centrul pozitiei ce reprezinta varful ciocului")] [SerializeField] Transform beakTip=null;
    [Tooltip("Radiusul in care este acceptata coliziunea cu ciocul")] [SerializeField] float beakTipRadius = 0.05f;
    [Tooltip("Obiect copil al agentului")] [SerializeField] GameObject beakFruit = null;

    [Header("Planta si Helperul")]
    [Tooltip("Scriptul folosit pentru a roti aleatoriu copacul")] [SerializeField] RandomRotationForPlant plant = null;
    [Tooltip("Scriptul folosit pentru a reaseza aleatoriu helperul")] [SerializeField] RandomPositionForHelper helper = null;

    [Header("Spatiu de antrenare")]
    [Tooltip("Componenta transform a spatiului de antrenare")] [SerializeField] Transform trainingGround = null;

    //  ---------------------------------------------------------- VARIABILE ----------------------------------------------------- //

    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    Rigidbody rb;

    // Folosite in netezirea rotatiilor 
    float smooth_Y_axis_change = 0f; // yaw
    float smooth_X_axis_change = 0f; // pitch 

    // Unghiul maxim de inclinare 
    const float max_X_axis_angle = 80f;

    // Observatii legate de cea mai apropriata tinta
    Vector3 closestTargetPosition = Vector3.zero;
    float distanceToClosestTarget = 0f;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    float timeGap = 0f;

    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    Vector3 targetedRayPos = Vector3.zero;

    // Tag-ul tintei
    string targetTagName = "";

    // Culoarea raycast-ului catre tinta
    Color rayColor = Color.cyan;

    // Daca a cules un fruct sau nu
    int pickedUpFruit = 0; // 0 - nu , 1 - da
 
    // ------------------------------------------------- METODE (Mostenite din) AGENT -------------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        // Resetare a componentei fizice
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;

        // Setare componente 
        ChangeTargetTag("preyFoodTree");
        pickedUpFruit = 0;
        beakFruit.SetActive(false);

        // Reseteaza pozitia agentului 
        ResetAgentPosition();
    }

    // Cod aplicat la inceputul unui episod
    public override void AgentReset()
    {
        // Reseteaza culoarea raycastului
        rayColor = Color.cyan;

        // Reseteaza tag 
        ChangeTargetTag("preyFoodTree");

        // Dezactiveaza fructul din cioc
        beakFruit.SetActive(false);

        // Reseteaza pickedUpFruit 
        pickedUpFruit = 0;

        // Reseteaza fortele aplicate asupra agentului
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reseteaza pozitia agentului 
        ResetAgentPosition();

        // Roteste planta
        plant.RotateFoodPlant();

        // Reamplaseaza helperul
        helper.ResetPosition();
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localRotation.normalized); // 1 Quaternion = 4 valori float 

        Vector3 beak_to_target = closestTargetPosition - beakTip.localPosition;
        AddVectorObs(beak_to_target.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(pickedUpFruit); // 1 valoare float

        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la searchProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        AddVectorObs(Vector3.Dot(beakTip.forward.normalized, beak_to_target.normalized)); // 1 valoare float intre [-1,1]

    // Total : 10 observatii (fara gameObject.localPosition , closestTarget.localPosition si beakTip.localPosition) -> Pot fi adaugate in viitor pentru consolidare
    }

    /// <summary>
    /// Pentru acest model vom folosi actiuni continue
    /// 
    /// vectorAction[i]:
    /// 0: miscare pe axa x (pozitiv - dreapta , negativ - stanga)
    /// 1: miscare pe axa y (pozitiv - sus , negativ jos)
    /// 2: miscare pe axa z (pozitiv - inainte , negativ inapoi)
    /// 3: unghiul de inclinare pozitiv - sus (spre cer) , negativ jos (spre sol) ) (rotatie pe axa lui x)
    /// 4: unghiul de giratie (pozitiv - rotatie spre dreapta , negativ - rotatie spre stanga) (rotatie pe axa lui y)
    /// 
    /// </summary>
    /// <param name="vectorAction"></param>
    public override void AgentAction(float[] vectorAction)
    {
        // Calculeaza vectorul de miscare - reprezinta o directie 
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        // Adaugarea fortei de miscare in directia aleasa
        rb.AddForce(move * moveSpeed);

        // Rotatia curenta
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // Calculeaza inclinarea si giratia 

        // Inclinare (Rotatie pe axa x)
        float X_axis_change = vectorAction[3];
        // Giratie (Rotatie pe axa Y)
        float Y_axis_change = vectorAction[4];

        // Calculeaza inclinarea si giratia netezite 

        // Inclinare 
        smooth_X_axis_change = Mathf.MoveTowards(smooth_X_axis_change , X_axis_change , 2f * Time.fixedDeltaTime);
        // Giratie 
        smooth_Y_axis_change = Mathf.MoveTowards(smooth_Y_axis_change, Y_axis_change, 2f * Time.fixedDeltaTime);

        // Calculare inclinare noua pe baza netezirii + limitarea unghiului
        float X_axis_rotation = rotationVector.x + smooth_X_axis_change * Time.fixedDeltaTime * xRotSpeed;
        if (X_axis_rotation > 180f) X_axis_rotation -= 360f;
        X_axis_rotation = Mathf.Clamp(X_axis_rotation, -max_X_axis_angle, max_X_axis_angle);

        // Calculare giratie
        float Y_axis_rotation = rotationVector.y + smooth_Y_axis_change * Time.fixedDeltaTime * yRotSpeed;

        // Aplica noua rotatie 
        transform.rotation = Quaternion.Euler(X_axis_rotation , Y_axis_rotation , 0f);
    }

    // Cand tipul de comportament este setat pe 'Heuristic' aceasta metoda este folosita pentru a 
    // Converti input de la un utilizator uman intr-un vector de actiuni ce poate fi *inteles* de reteaua neuronala 
    public override float[] Heuristic()
    {
        // Valorile initiale , daca la un apel al functiei butonul aferent unei directii nu a fost apasat 
        // se va folosi valoarea initiala pentru acel vector directie / valoare de rotatie  

        Vector3 forward = Vector3.zero;  // +1 inainte , -1 inapoi
        Vector3 left = Vector3.zero; // +1 stanga , -1 dreapta
        Vector3 up = Vector3.zero; // +1 sus  , -1 jos
        float X_axis_rotation = 0f;
        float Y_axis_rotation = 0f;

        // Converteste inputul de la tastatura in miscare si rotire 
        // Desi agentul va lua actiuni continue (valori in intervalul (-1,1)) noi putem oferi doar valori discrete prin intermediul tastaturii 
        // In cazul de fata vom folosi valori din multimea {-1,0,1}

        // Inainte / Inapoi
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // Stanga / Dreapta 
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // Sus / Jos
        if (Input.GetKey(KeyCode.Q)) up = transform.up;
        else if (Input.GetKey(KeyCode.E)) up = -transform.up;

        // Inclinare sus / jos
        if (Input.GetKey(KeyCode.UpArrow)) X_axis_rotation = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) X_axis_rotation = -1f;

        // Giratie spre stanga / dreapta 
        if (Input.GetKey(KeyCode.LeftArrow)) Y_axis_rotation = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) Y_axis_rotation = 1f;

        // Combina vectorii pentru a obtine un vector directie final normalizat
        Vector3 combined = (forward + up + left).normalized;

        // Put the actions into an array and return
        return new float[] { combined.x , combined.y , combined.z , X_axis_rotation , Y_axis_rotation };
    }

    // -------------------------------------------------------- METODE ----------------------------------------------------------- //

    // Redenumire a tagului pentru tinta agentului
    void ChangeTargetTag(string newTagName) => targetTagName = newTagName;

    // Sistem de reasezare a agentului

    void ResetAgentPosition()
    {
        // Resetarea pozitiei - Inaltime aleatorie (axa y) - Una dintre 3 pozitii (axele x,z) 
        int posIndex = Random.Range(0, 3);
        switch (posIndex)
        {
            case 0:
                gameObject.transform.position = new Vector3(trainingGround.position.x-29f, Random.Range(4.5f, 12f), trainingGround.position.z - 16.5f);
                break;
            case 1:
                gameObject.transform.position = new Vector3(trainingGround.position.x - 27f, Random.Range(4.5f, 12f), trainingGround.position.z - 9f);
                break;
            case 2:
                gameObject.transform.position = new Vector3(trainingGround.position.x - 27f, Random.Range(4.5f, 12f), trainingGround.position.z - 25.5f);
                break;
        }

        // Rotatie aleatorie 
        int rotationIndex = Random.Range(0, 3);
        if (rotationIndex != 0)
            transform.rotation = Quaternion.Euler(transform.rotation.x, Random.Range(40f, 140f), transform.rotation.y);
        else
            transform.rotation = Quaternion.Euler(transform.rotation.x, Random.Range(-40f, -140f), transform.rotation.y);
    }

    // Sistem de cautare - cu optimizare . (apelata de 10 ori pe secunda in loc de ~ 60)

    private void Update()
    {
        OptimizedCheckInRadius(rayColor);
    }

    // Optimizeaza (reduce numarul de utilizari) ale metodei de cautare in proximitate ( metoda foarte "grea" )
    protected virtual void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - timeGap >= 0.1f)
        {
            // Verificam cea mai apropriata tinta (if any) si stocam informatii legate despre ea
            CheckTargetInProximity();
            timeGap = Time.time;

            // Verificam daca agentul a terminat sarcina
            CheckIfFoodWasDelivered();

            // Aplicam -mic reward o data la 0.1s in functie de distanta pana la tinta
            AddReward(Mathf.Min(-distanceToClosestTarget, 0f) / maxStep);
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
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTagName);
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

    // Verifica daca a intr-at intr-un anumit radius fata de helper pentru a "livra" mancarea
    // O functie predefinita va da drop la mancare in viitor
    void CheckIfFoodWasDelivered()
    {
        if(targetTagName == "helper" && distanceToClosestTarget <= deliveryDistanceRequired)
        {
            // Agent
            beakFruit.SetActive(false);

            // Invatare
            SetReward(1f);
            Done();
        }
    }

    // Functia de reward - Partea ce tine de coliziuni

    // Coliziuni cu triggere 
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("boundary"))
        {
            SetReward(-1f);
            Done();
        }

        //  coliziunea cu fructe (cand nu are fruct cules / cand are )
        if (other.gameObject.CompareTag("preyFoodTree"))
        {
            // Verificam inainte daca agentul culege fructul cu ciocul (nu vrem sa se atinga cu aripa si fructul sa fie cules)
            Vector3 closestPointToBeakTip = other.ClosestPoint(beakTip.position);
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < beakTipRadius)
            {
                if (pickedUpFruit == 0)
                {
                    AddReward(1f);
                    ChangeTargetTag("helper");
                    rayColor = Color.yellow;
                    pickedUpFruit = 1;
                    beakFruit.SetActive(true);
                }
                else
                {
                    AddReward(-0.2f);
                }
            }
        }
    }

    // Coliziuni cu obiecte rigide
    private void OnCollisionEnter(Collision other)
    {
        // Coliziunea cu pamantul
        if(other.gameObject.CompareTag("Ground"))
        {
            SetReward(-1f);
            Done();
        }
    }
}
