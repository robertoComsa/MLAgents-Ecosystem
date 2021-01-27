using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class AerialPhaorisAgent_Training : Agent
{
    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] float moveSpeed = 100f;
    [Tooltip("Viteza de giratie (rotire in jurul axei y)")] [SerializeField] float yRotSpeed = 100f;  // yaw
    [Tooltip("Viteza de inclinare (rotire in jurul axei z)")] [SerializeField] float xRotSpeed = 100f;  // pitch
    [Tooltip("Distanta de cautare")] [SerializeField] float searchProximity = 40f;
    [Tooltip("Proximitatea de livrare")] [SerializeField] float deliveryDistanceRequired = 5f;

    [Header("Ciocul pasarii")]
    [Tooltip("Centrul pozitiei ce reprezinta varful ciocului")] [SerializeField] Transform beakTip = null;
    [Tooltip("Radiusul in care este acceptata coliziunea cu ciocul")] [SerializeField] float beakTipRadius = 0.05f;
    [Tooltip("Obiect copil al agentului")] [SerializeField] GameObject beakFruit = null;

    [Header("Fruct")]
    [Tooltip("Fructul ce trebuie aruncat in jurul agentilor galvadon")] [SerializeField] GameObject dropFruit = null;

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
    Vector3 closestTargetGlobalPosition = Vector3.zero;

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

    // Valoarea vectorului dot dintre cioc si tinta 
    float beakToTargetDotValue = 0f;

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

        // Replaseaza la inaltimea corecta 
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, 13.7f, gameObject.transform.position.z);
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        // Rotatia agentului (raportat la obiectul parinte)
        AddVectorObs(gameObject.transform.localRotation.normalized); // 1 quaternion = 4 valori float

        // -- 

        // Pozitia agentului ( raportat la obiectul parinte ) ------ ADAUGAT IN PHAORIS_04
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector 3 = 3 valori float
        // Un vector ce indica pozitia celei mai apropriate tinte (de asemenea raportata la obiectul parinte - care este comun)
        AddVectorObs(closestTargetPosition.normalized); // 1 vector3 = 3 valori float

        // -- 

        // Un vector ce indica directia *inainte* a ciocului (respectiv agent)
        AddVectorObs(beakTip.forward.normalized); // 1 vector3 = 3 valori float
        // Un vector ce indica directia de la cioc la tinta
        Vector3 beak_to_target = closestTargetGlobalPosition - beakTip.position;
        AddVectorObs(beak_to_target.normalized); // 1 vector3 = 3 valori float
        // Un dot product - valori negative cand agentul e cu spatele la fructe , valori pozitive cand agentul e cu fata la fructe 
        beakToTargetDotValue = Vector3.Dot(beakTip.forward.normalized, beak_to_target.normalized);
        AddVectorObs(Vector3.Dot(beakTip.forward.normalized, beak_to_target.normalized)); // 1 valoare float intre [-1,1]

        // --

        // Un int ce actioneaza ca un bool si verifica daca agentul a cules un fruct 
        AddVectorObs(pickedUpFruit); // 1 valoare int
        // Distanta pana la cea mai apropriata tinta
        AddVectorObs(distanceToClosestTarget / searchProximity); // 1 valoare float; impartim la searchProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)
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

        ///
        /// SA FAC CLAMP INTRE -1 ȘI 1 LA VALORI (VEZI EXEMPLU PT CONTINUU https://github.com/Unity-Technologies/ml-agents/blob/release-0.14.0/docs/Learning-Environment-Design-Agents.md
        ///

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
        smooth_X_axis_change = Mathf.MoveTowards(smooth_X_axis_change, X_axis_change, 2f * Time.fixedDeltaTime);
        // Giratie 
        smooth_Y_axis_change = Mathf.MoveTowards(smooth_Y_axis_change, Y_axis_change, 2f * Time.fixedDeltaTime);

        // Calculare inclinare noua pe baza netezirii + limitarea unghiului
        float X_axis_rotation = rotationVector.x + smooth_X_axis_change * Time.fixedDeltaTime * xRotSpeed;
        if (X_axis_rotation > 180f) X_axis_rotation -= 360f;
        X_axis_rotation = Mathf.Clamp(X_axis_rotation, -max_X_axis_angle, max_X_axis_angle);

        // Calculare giratie
        float Y_axis_rotation = rotationVector.y + smooth_Y_axis_change * Time.fixedDeltaTime * yRotSpeed;

        // Aplica noua rotatie 
        transform.rotation = Quaternion.Euler(X_axis_rotation, Y_axis_rotation, 0f);
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
        return new float[] { combined.x, combined.y, combined.z, X_axis_rotation, Y_axis_rotation };
    }

    // -------------------------------------------------------- METODE ----------------------------------------------------------- //


    // Redenumire a tagului pentru tinta agentului
    void ChangeTargetTag(string newTagName) => targetTagName = newTagName;

    private void Update()
    {
     
    }

    // FixedUpdate - Apelata de 50 de ori pe secunda (nu tine cont de frameRate-ul aplicatiei ; Buna pentru componente fizice)
    private void FixedUpdate()
    {
        // Sistem de cautare - cu optimizare . (apelata de 10 ori pe secunda)
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

            // Reward pentru directia in care se uita agentul ( 1 - maxim cand se uita direct la tinta , -1 - minim cand se uita in directia opusa)
            AddReward(0.01f * beakToTargetDotValue);

            // Reward pentru distanta fata de tinta.
            AddReward(-0.01f * distanceToClosestTarget / searchProximity);
        }

            DrawLine(transform.position, targetedRayPos, rayColor);
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
            closestTargetGlobalPosition = closestTarget.transform.position;
            closestTargetPosition = closestTarget.transform.localPosition;
            distanceToClosestTarget = nearestDistance;
            targetedRayPos = closestTarget.transform.position;
        }
        else
        {
            closestTargetGlobalPosition = Vector3.zero;
            closestTargetPosition = Vector3.zero;
            distanceToClosestTarget = 40f;
            targetedRayPos = Vector3.zero;
        }
    }

    // Verifica daca a intrat intr-un anumit radius fata de helper pentru a "livra" mancarea
    // O functie predefinita va da drop la mancare in viitor
    void CheckIfFoodWasDelivered()
    {
        if (targetTagName == "prey" && distanceToClosestTarget <= deliveryDistanceRequired)
        {
            // Agent
            beakFruit.SetActive(false);
            // Reseteaza tag 
            ChangeTargetTag("preyFoodTree");
            // Reseteaza picked up fruit
            pickedUpFruit = 0;
            // Schimba culoarea razei
            rayColor = Color.cyan;

            // Da drumul la fruct
            Instantiate(dropFruit, beakFruit.transform.position, Quaternion.identity);

            // Reward pentru livrarea fructului 
            AddReward(1f);
        }
    }

    // --------------------------------------------------------------------- COLIZIUNI ---------------------------------------------------- //

    // Coliziuni cu triggere 
    private void OnTriggerEnter(Collider other)
    {
        //  coliziunea cu fructe (cand nu are fruct cules / cand are )
        if (other.gameObject.CompareTag("preyFoodTree"))
        {
            // Verificam inainte daca agentul culege fructul cu ciocul (nu vrem sa se atinga cu aripa si fructul sa fie cules)
            Vector3 closestPointToBeakTip = other.ClosestPoint(beakTip.position);
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < beakTipRadius && pickedUpFruit == 0)
            {
                ChangeTargetTag("prey");
                rayColor = Color.yellow;
                pickedUpFruit = 1;
                beakFruit.SetActive(true);


                // Reward pentru culegerea fructului 
                AddReward(1f);
            }
        }


    }

    private void OnCollisionEnter(Collision other)
    {
        // Penalizare pentru coliziuni
        if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("boundary"))
        {
            SetReward(-1f);
            Done();
        }
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
}
