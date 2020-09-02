using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class Galvadon2 : Agent
{
    // VARIABILE VIZIBILE IN EDITOR

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] private float moveSpeed = 0f;
    [Tooltip("Viteza de rotatie")] [SerializeField] private float rotationSpeed = 0f;
    [Tooltip("Distanta in care pradatorul vaneaza")] [SerializeField] private float searchTargetProximity = 0f;
   
    // VARIABILE

    // Pozitia de start (folosita in reasezarea agentului in scena)
    private Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    private Rigidbody rb;
    // int care reprezinta targetul pe care il cautam ( 0 - mancare pentru agent , 1 - mancare pentru erbivori , 2 - erbivori )
    private int target = 0;
    // Folosit pentru a apela reinstantierea hranei in scena 
    private FoodPickUp preyFood = null;

    // Observatii legate de cel mai apropriat obiect tinta
    private Vector3 closestTargetPosition = Vector3.zero;
    private float distanceToClosestTarget = 0f;
    // Utilizate pentru vizualizarea celui mai apropriat obiect tinta
    private Color rayColor = Color.magenta;
    private string tagName = string.Empty;

    // Verificam daca agentul cara hrana
    private bool carryingFood = false;
    public bool GetCarryingFood() { return carryingFood; }


    // METODE (Mostenite din) AGENT

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
        target = Random.Range(0, 3);
    }

    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.localPosition.normalized); // 1 vector3 = 3 valori float

        AddVectorObs(gameObject.transform.forward.normalized); // 1 Vector3 = 3 valori float

        AddVectorObs(distanceToClosestTarget / searchTargetProximity); // 1 valoare float; impartim la huntProximity (valoarea maxima pe care o poate lua distance to closestPrey pentru normalizare)

        Vector3 toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        AddVectorObs(toClosestTarget.normalized); // 1 Vector3 = 3 valori float

        // O valoare int ce poate fi 0 sau 1 si actioneaza ca un boolean dar este int pentru ca vrem sa il oferim ca observatie numerica
        AddVectorObs(target); // 1 valoare int

        // Total: 11 + Observatiile de tip raycast
    }

    /// <summary>
    /// Alege actiuni pe baza unui vector ( de valori discrete )
    /// Index 0: Decide daca agentul se misca (valori disrecte; 0 - sta pe loc , 1 inainteaza misca )
    /// Index 1: Decide daca agentul se roteste (0 -> Nu se roteste; 1 -> se roteste la stanga; 2 -> se roteste la dreapta)
    /// Index 2: Decide daca schimba creierul sau nu.
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

        // Aplica miscarea asupra agentului cu valorile alese (primele doua actiuni)
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
        if (preyFood != null) preyFood.ResetFood();
        target = Random.Range(0,3);
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
        if (distanceToClosestTarget != 0)
            AddReward(-(distanceToClosestTarget / searchTargetProximity) / maxStep);
        else AddReward(-1f / maxStep);

        // 6 erbivori x 0.2 = 1.2 reward maxim (daca agentul nu ar apuca sa fie penalizat cu -0.01 pe step)
        // Ideal vrem sa antrenam pana cand agentul are reward in jur de 1 (instantierea aleatorie a pradei va face ca rezultatul sa varieze)

        CheckTargetInProximity();
    }

    /// <summary>
    /// Verifica daca exista erbivori / prada intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    public void CheckTargetInProximity()
    {
        ManageTargetTag();

        GameObject[] targets = GameObject.FindGameObjectsWithTag(tagName);

        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        foreach (GameObject actualTarget in targets)
        {
            float distance = Vector3.Distance(transform.position, actualTarget.transform.position);

            if (distance < nearestDistance && distance < searchTargetProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
            {
                switch (target)
                {
                    case 0:
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = actualTarget;
                        break;
                    case 1:
                        if (actualTarget.GetComponent<FoodPickUp>().getIsPickedUp() == false) // Altfel exista cazuri in care cea mai apropriata bucata                                                                                             
                        {                                                                     // este deja carata de alt agent 
                            targetInRadius = true;
                            nearestDistance = distance;
                            closestTarget = actualTarget;
                        }
                        break;
                    case 2:
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = actualTarget;
                        break;
                }
            }
        }
        if (targetInRadius)
        {
            Debug.DrawLine(transform.position, closestTarget.transform.position, rayColor);
            closestTargetPosition = closestTarget.transform.localPosition;
            distanceToClosestTarget = nearestDistance;
        }
        else
        {
            closestTargetPosition = Vector3.zero;
            distanceToClosestTarget = 0f;
        }
    }

    // Seteaza tag-ul si culoarea razei pentru functia de cautare pe baza tintei
    private void ManageTargetTag()
    {
        switch (target)
        {
            case 0:
                tagName = "galvadonFood";
                rayColor = Color.magenta;
                break;
            case 1:
                tagName = "preyFood";
                rayColor = Color.cyan;
                break;
            case 2:
                tagName = "prey";
                rayColor = Color.green;
                break;
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
        // Daca ne lovim de margini
        if (other.CompareTag("boundary"))
        {
            SetReward(-1f);
            Done();
        }

        // Daca ne lovim de mancarea agentului
        if (other.gameObject.CompareTag("galvadonFood")) 
        {
            if (target == 0)
            {
                AddReward(1f);
                Done();
            }
            else if (target != 0)
            {
                AddReward(-0.2f);
                Done();
            }
        }

        // Daca ne lovim de mancarea erbivorilor 
        if (other.gameObject.CompareTag("preyFood"))// && carryingFood == false
        {
            // Folosite in viitor
            //preyFood = other.gameObject.GetComponent<FoodPickUp>();
            //preyFood.GetPickedUp(gameObject);
            //carryingFood = true;
            if (target == 1)
            {
                AddReward(1f);
                Done();
            }
            else if(target!=1)
            {
                AddReward(-0.2f);
                Done();
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("prey"))// && carryingFood == true)
        {
            // Folosite in viitor 
            //preyFood.gameObject.SetActive(false); 
            //StartCoroutine(WaitToGiveFood());
            if (target == 2)
            {
                AddReward(1f);
                Done();
            }
            else if(target!=2)
            {
                AddReward(-0.2f);
                Done();
            }
        }
    }

    // Existau cazuri in care carryingFood devenea fals inainte ca erbivorul sa primeasca informatia , si astfel erbivorul nu mai incepea rutina de a crea floare
    IEnumerator WaitToGiveFood()
    {
        yield return new WaitForSeconds(0.1f); 
        carryingFood = false;
    }
}

