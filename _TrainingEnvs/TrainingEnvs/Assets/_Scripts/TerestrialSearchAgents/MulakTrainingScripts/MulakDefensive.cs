using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class MulakDefensive : Agent
{
    // <>--<> VARIABILE VIZIBILE IN EDITOR <>--<>

    [Header("Parametrii deplasare")]
    [Tooltip("Viteza de inaintare")] [SerializeField] protected float jumpForce = 0f;

    // <>--<> VARIABILE <>--<>

    // Pozitia de start (folosita in reasezarea agentului in scena)
    Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    Rigidbody rb;
    // Boolean care ne spune daca agentul este pe pamant sau in aer
    bool isGrounded = true;
    // Directia sariturii
    Vector3 jumpDirection = new Vector3(1f, 1.5f, -1f);

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
    }

    // Cod aplicat la inceputul unui episod (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        gameObject.transform.position = startingPosition;
        rb.velocity = Vector3.zero;
    }

    //
    public override void AgentAction(float[] vectorAction)
    {
        jumpDirection = new Vector3(Mathf.Clamp(vectorAction[0], -1, 1),
                                    Mathf.Clamp(vectorAction[1], 1, 2),
                                    Mathf.Clamp(vectorAction[2], -1, 1));

        if(isGrounded)
        {
            rb.AddForce(jumpDirection * Mathf.Clamp(vectorAction[4], 1, 4), ForceMode.Impulse);
            isGrounded = false;
        }

        // Recompensa mica pentru existenta
        AddReward(1 / maxStep); // If it does not die he can get a maximum reward of 1
    }
    /*
    /// <returns> Returneaza vectorul de valori vectorAction (format de data aceasta de input uman, nu de reteaua neuronala ) </returns>
    public override float[] Heuristic()
    {
        
        return new float[] {  };
    }
    */
    // ------------------------------------------------- METODE DEFENSIVE AGENT ----------------------------------------------- //

    // Apelata o singura data inainte de start.
    protected virtual void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // Apelata in fiecare frame
    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

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

    // Cand agentul loveste un obiect limita
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boundary"))
        {
            AgentReset();

            // Penalizare
            AddReward(-0.1f);
        }    
    }

    // Cand agentul este prins de un carnivor
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("predator"))
        {
            AgentReset();

            // Penalizare
            AddReward(-0.1f);
        }
    }

}
