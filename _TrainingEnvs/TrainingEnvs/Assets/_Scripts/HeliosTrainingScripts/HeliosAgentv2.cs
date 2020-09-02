using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class HeliosAgentv2 : Agent
{
    // VARIABILE VIZIBILE IN EDITOR

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] private float moveSpeed = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] private float rotationSpeed = 0f;
    [Tooltip("Distanta in care pradatorul vaneaza")] [SerializeField] private float huntProximity = 0f;

    // VARIABILE

        // Pozitia de start (folosita in reasezarea agentului in scena)
    private Vector3 startingPosition = Vector3.zero;
        // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    private Rigidbody rb;
        // Contor pentru numarul de erbivori mancati
    private int preyCount = 0;

        // Observatii legate de cel mai apropriat pradator
    private Vector3 closestPreyPosition = Vector3.zero;
    private float distanceToClosestPrey = 0f;

    // METODE (Mostenite din) AGENT

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
        preyCount = 0;
    }

    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float
        
        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestPrey / huntProximity); // 1 valoare float; impartim la huntProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        AddVectorObs(closestPreyPosition.normalized); // 1 Vector3 = 3 valori float

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

    // Reseteaza agentul si spatiul de antrenare 
    public override void AgentReset()
    {
        PlaceRandomly(6f);
        preyCount = 0;
    }

    // METODE

    // Apelata o singura data inainte de start.
    private void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // FixedUpdate este apelata o data la 0.02 secunde (50 de apeluri pe secunda; independent de fps)
    private void FixedUpdate()
    {
        // Incurajam actiunea - Eliminat pentru Heliosv2_02
        // AddReward(-1f / maxStep); // x 50 de apeluri -> -0.01s pe secunda
        if (distanceToClosestPrey != 0)
            AddReward(-(distanceToClosestPrey / huntProximity) / maxStep);
        else AddReward(-1f/maxStep);

        // Incheiem episodul la 6 erbivori mancati
        if (preyCount >= 6)
            Done();
        // 6 erbivori x 0.2 = 1.2 reward maxim (daca agentul nu ar apuca sa fie penalizat cu -0.01 pe step)
        // Ideal vrem sa antrenam pana cand agentul are reward in jur de 1 (instantierea aleatorie a pradei va face ca rezultatul sa varieze)

        CheckPreyInProximity();
    }


    /// <summary>
    /// Verifica daca exista erbivori / prada intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    public void CheckPreyInProximity()
    {
        GameObject[] preys = GameObject.FindGameObjectsWithTag("prey");
        float nearestDistance = Mathf.Infinity;
        GameObject closestPrey = null;
        bool preyInRadius = false;

        foreach (GameObject prey in preys)
        {
            float distance = Vector3.Distance(transform.position, prey.transform.position);

            if (distance < nearestDistance && distance < huntProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
            {
                preyInRadius = true;
                nearestDistance = distance;
                closestPrey = prey;
            }
        }

        if (preyInRadius)
        {
            Debug.DrawLine(transform.position, closestPrey.transform.position, Color.blue);
            closestPreyPosition = closestPrey.transform.localPosition;
            distanceToClosestPrey = nearestDistance;
        }
        else
        {
            closestPreyPosition = Vector3.zero;
            distanceToClosestPrey = 0f;
        }
    }

    // Plaseaza agentul aleatoriu in spatiul de antrenare. (Atat pozitie cat si rotatie)
    public void PlaceRandomly(float value)
    {
        transform.position = new Vector3(startingPosition.x + Random.Range(-value, value), startingPosition.y, startingPosition.z + Random.Range(-value, value));
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        transform.rotation = newRotation;
    }


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
        if (other.gameObject.CompareTag("prey"))
        {
            AddReward(0.2f);
            preyCount++;
        }
    }
}
