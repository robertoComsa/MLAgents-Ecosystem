using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class MulakDefensive : Agent
{
    // <>--<> VARIABILE VIZIBILE IN EDITOR <>--<>

    [Header("Parametri")]
    [Tooltip("Forta sariturii")] [SerializeField] protected float jumpForceMultiplier = 0f;
    [Tooltip("Radiusul in care verificam daca sunt pradatori")] [SerializeField] protected float radius = 0f;
    [Tooltip("Componenta RB a parintelui")] [SerializeField] private Rigidbody agentRB = null;
    [Tooltip("Agent collision logics")] [SerializeField] private MulakAgentCollisionLogics agentCollisionLogics = null;

    // <>--<> VARIABILE <>--<>

    // Pozitia de start (folosita in reasezarea agentului in scena)
    Vector3 startingPosition = Vector3.zero;
    // Directia sariturii
    Vector3 jumpDirection = new Vector3(1f, 1.5f, -1f);
    // Timpul dintre sarituri
    private float timeGap = 0f;
    // boolean care ne spune daca agentul poate sari
    private bool jumpAllowed = false;
    // int folosit pe post de boolean - daca este un pradator in radius sau nu
    private int predatorInsideRadius = 0; // 0 - no , 1 - yes

    // boolean ce verifica daca a inceput simularea 
    protected bool simStarted = false;

    // ------------------------------------------------- METODE (Mostenite din) AGENT ---------------------------------------- //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();

        agentRB.centerOfMass = Vector3.zero;
        agentRB.inertiaTensorRotation = Quaternion.identity;

        // Disabling collision for placement purposes 
        agentRB.isKinematic = true;
    }

    // Cod aplicat la inceputul unui episod (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        gameObject.transform.position = startingPosition;
        agentRB.velocity = Vector3.zero;
        predatorInsideRadius = 0;
    }

    public override void CollectObservations()
    {
        AddVectorObs(predatorInsideRadius); // 1 Int
    }

    //
    public override void AgentAction(float[] vectorAction)
    {
        jumpDirection = new Vector3(Mathf.Clamp(vectorAction[0], -1, 1),
                                    Mathf.Clamp(vectorAction[1], 1, 2),
                                    Mathf.Clamp(vectorAction[2], -1, 1));

        if(agentCollisionLogics.GetAgentGrounded() && jumpAllowed && predatorInsideRadius==1)
        {
            agentRB.AddForce(jumpDirection * Mathf.Clamp(vectorAction[3], 0, 1) * jumpForceMultiplier, ForceMode.Impulse);
            agentCollisionLogics.SetAgentGrounded(false);
            jumpAllowed = false;
        }
    }

    public override float[] Heuristic()
    {
        return base.Heuristic();
    }

    public void RewardAgent()
    {
        if (predatorInsideRadius == 0)
            AddReward(10 / 5000); 
    }


    // ------------------------------------------------- METODE DEFENSIVE AGENT ----------------------------------------------- //

   
    // Allow jumping once 1s
    private void AllowJump()
    {
        if (Time.time - timeGap >= 3f)
        {
            jumpAllowed = true;
            timeGap = Time.time;
        }
    }

    // Metoda de verificare a pradatorilor intr-un radius
    private void CheckPredatorInRadius(float radius)
    {
        // Ne intereseaza daca cel putin 1 pradator este in radius
        Collider[] predatorCollider = new Collider[1];

        Physics.OverlapSphereNonAlloc(gameObject.transform.position,radius,predatorCollider,9);

        if (predatorCollider[0] != null)
        {
            //Debug.Log(predatorCollider[0].gameObject.tag);
            predatorInsideRadius = 1;
        }
        else
        {
            //Debug.Log("Not in radius");
            predatorInsideRadius = 0;
        }
        
    }

    // Apelata o singura data inainte de start.
    protected virtual void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // Apelata in fiecare frame
    private void FixedUpdate()
    {
        // Control uman 
        if (Input.GetKeyDown(KeyCode.Space) && agentCollisionLogics.GetAgentGrounded())
        {
            agentRB.AddForce(jumpDirection * jumpForceMultiplier, ForceMode.Impulse);
            agentCollisionLogics.SetAgentGrounded(false);
        }

        // Permite sarituri o data la n secunde
        AllowJump();

        // Verifica daca exista un pradator in raza de siguranta
        CheckPredatorInRadius(radius);

        // Reward (Daca nu exista pradator in radius agentul primeste: 0.02 per frame x 50 (fixed frame/s) = 0,1/s 
        RewardAgent();

        // Permitem agentului sa ia decizii 
        if (GameManager.Instance.CanAgentsRequestDecisions == true)
        {
            RequestDecision();
            if (simStarted == false)
            {
                agentRB.isKinematic = false;
                agentRB.detectCollisions = true;
                simStarted = true;
            }
        }

    }

    /*

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
            AgentReset();

            // Penalizare
            AddReward(-0.1f);
        }
    }

    // Cand agentul intra intr-o coliziune fizica
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("predator"))
        {
            AgentReset();

            // Penalizare
            AddReward(-0.1f);
        }
    }
    */

}
