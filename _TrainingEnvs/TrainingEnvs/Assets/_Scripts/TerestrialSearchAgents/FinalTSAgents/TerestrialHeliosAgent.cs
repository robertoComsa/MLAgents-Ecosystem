using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

[System.Serializable]
public class TerestrialHeliosAgent : TerestrialSearchAgent
{
    // ------------------------------------------------- MOSTENITE DIN TERESTRIAL SEARCH AGENT --------------------------- //

    // Redenumirea tintei
    protected override void Awake()
    {
        base.Awake();
        RenameTag("prey");
    }

    protected override void OptimizedCheckInRadius(Color rayColor)
    {
        if (Time.time - proximitySearchTimeGap >= 0.1f)
        {
            CheckTargetInProximity();
            proximitySearchTimeGap = Time.time;

            // Reward pentru directia in care se uita agentul ( 1 - maxim cand se uita direct la tinta , -1 - minim cand se uita in directia opusa)
            AddReward(0.1f * Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized));

            // Reward pentru distanta fata de tinta.
            AddReward(-0.1f * distanceToClosestTarget / searchProximity);
        }

        if (GameManager.Instance.GetRaysEnabled() == true)
            DrawLine(transform.position, targetedRayPos, rayColor);
    }

    public override void CollectObservations()
    {
        base.CollectObservations();

        toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        // Un produs dot intre directia in care se uita agentul si directia in care se afla cea mai apropriata tinta
        AddVectorObs(Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized)); // 1 valoare float
    }

    // ------------------------------------------------------------- METODE -------------------------------------------------- //

    protected override void StarvingProcess()
    {
        base.StarvingProcess();

        Debug.Log(starvingInterval);

        if (starvingInterval <= 0f)
        {
            // Distrugem acest agent (moare de foame)
            Destroy(gameObject);

            // Modificam datele simularii
            StatisticsManager.Instance.ModifySimData("heliosStarved");
            StatisticsManager.Instance.ModifyAgentsNumber("remove" , "Helios");
        }
    }

    // -- Pauza (inlocuitor animatie , atunci cand prinde un erbivor)

    IEnumerator EatingPause()
    {
        float aux = moveSpeed;
        moveSpeed = 0;
        rb.freezeRotation = true;
        yield return new WaitForSeconds(2.5F);
        rb.freezeRotation = false;
        moveSpeed = aux;
    }

    // --- Reward system for Heliosv3_02 training in Big Environment

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("prey"))
        {
            AddReward(0.5f);
            StartCoroutine(EatingPause());

            // Mananca (starving system)
            Eat();

            // Noteaza in simData
            StatisticsManager.Instance.ModifySimData("mulaksEaten");
            StatisticsManager.Instance.ModifyAgentsNumber("remove", "Mulak");
        }

        // NU RESET
        if(other.gameObject.CompareTag("boundary"))
        {
            AddReward(-0.5f);
            AgentReset();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // NU RESET
        if(other.CompareTag("boundary"))
        {
            AddReward(-0.5f);
            AgentReset();
        }

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
}
