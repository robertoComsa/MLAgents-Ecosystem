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

    public override void CollectObservations()
    {
        base.CollectObservations();

        Vector3 toClosestTarget = closestTargetPosition - gameObject.transform.localPosition;
        // Un produs dot intre directia in care se uita agentul si directia in care se afla cea mai apropriata tinta
        AddVectorObs(Vector3.Dot(gameObject.transform.forward.normalized, toClosestTarget.normalized)); // 1 valoare float
    }
}
