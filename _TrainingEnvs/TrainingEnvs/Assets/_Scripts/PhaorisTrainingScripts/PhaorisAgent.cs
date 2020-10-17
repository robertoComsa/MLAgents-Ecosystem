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

    [Header("Ciocul pasarii")]
    [Tooltip("Centrul pozitiei ce reprezinta varful ciocului")] [SerializeField] Transform beakTip;

    //  ---------------------------------------------------------- VARIABILE ----------------------------------------------------- //

    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    Rigidbody rb;

    // Folosite in netezirea rotatiilor 
    float smooth_Y_axis_change = 0f; // yaw
    float smooth_X_axis_change = 0f; // pitch 

    // Unghiul maxim de inclinare 
    const float max_X_axis_angle = 80f;

    // Observatii legate de cea mai apropriata tinta
    protected Vector3 closestTargetPosition = Vector3.zero;
    protected float distanceToClosestTarget = 0f;

    // Folosita pentru a optimiza (mai putine utilizari) ale metodei de cautare in proximitate
    protected float timeGap = 0f;

    // Agentilor le-am oferit localPosition , dar noi vrem sa tragem raycasturi pana la position  
    protected Vector3 targetedRayPos = Vector3.zero;

    // Tag-ul tintei
    protected string tagName = "";

    // ------------------------------------------------- METODE (Mostenite din) AGENT -------------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
    }

    // Cod aplicat la inceputul unui episod
    public override void AgentReset()
    {
        // Reseteaza tag 
        ChangeTag("preyFoodTree");

        // Reseteaza fortele aplicate asupra agentului
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {

    }

    /// <summary>
    /// Pentru acest model vom folosi actiuni continue
    /// 
    /// vectorAction[i]:
    /// 0: miscare pe axa x (+1 - dreapta , -1 stanga)
    /// 1: miscare pe axa y (+1 - sus , -1 jos)
    /// 2: miscare pe axa z (+1 - inainte , -1 inapoi)
    /// 3: unghiul de inclinare (+1 - sus (spre cer) , -1 jos (spre sol) ) (rotatie pe axa lui x)
    /// 4: unghiul de giratie (+1 - rotatie spre dreapta , -1 - rotatie spre stanga) (rotatie pe axa lui y)
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
    void ChangeTag(string newTagName) => tagName = newTagName;

    // Spawn system 

    // Searching system - with optimization and checking in update 

    // Reward system 
}
