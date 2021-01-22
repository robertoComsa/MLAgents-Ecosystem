using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MulakAgentCollisionLogics : MonoBehaviour
{
    private Vector3 startingPosition = Vector3.zero;

    private void Awake() {startingPosition = gameObject.transform.position;}

    public void ResetPosition() {gameObject.transform.position = startingPosition;}

    private bool agentGrounded = true;
    public bool GetAgentGrounded() { return agentGrounded; }
    public void SetAgentGrounded(bool value) { agentGrounded = value; }

    // Checking collision and trigger logics

    // Cand agentul intra intr-o coliziune fizica
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("predator"))
            //Destroy(gameObject);
            ResetPosition();
    }

    // Cand agentul atinge un trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boundary"))
            ResetPosition();

        // Verifica daca a intrat intr-o coliziune ; daca da interzice amplasarea
        CheckIfAgentIsPlaceable(false, other);
    }

    // Cat timp atinge pamantul agentul "isGrounded"
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            agentGrounded = true;
    }

    // Cand agentul paraseste pamantul nu mai este grounded
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            agentGrounded = false;
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

    // Functie de verificare a colliderului folosita la amplasarea agentilor
    protected bool CheckColliderTag(Collider other)
    {
        if (other.CompareTag("predator") || other.CompareTag("prey") || other.CompareTag("helper") || other.CompareTag("Untagged") || other.CompareTag("boundary"))
            return true;

        return false;
    }

    // Metoda care verifica daca suntem in modul de amplasare si permite/interzice amplasarea agentilor in functie de coliziuni cu obiecte
    protected void CheckIfAgentIsPlaceable(bool allowPlacement, Collider other)
    {
        if (GameManager.Instance.CanAgentsRequestDecisions == false && CheckColliderTag(other) == true) // Inseamna ca e in placing mode
            // permitem sau interzicem amplasarea
            PlacementController.Instance.CanPlaceAgents = allowPlacement;
    }

    private void Update()
    {
        // Daca simularea s-a incheiat distrugem acest agent
        if (GameManager.Instance.SimulationEnded)
            Destroy(gameObject);
    }
    

}
