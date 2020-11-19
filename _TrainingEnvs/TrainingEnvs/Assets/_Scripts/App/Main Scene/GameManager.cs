using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public bool CanAgentsRequestDecisions { get; set; } = false;

    private void Update()
    {
        // Activate agents

        if (Input.GetKeyDown(KeyCode.M))
            CanAgentsRequestDecisions = true;
    }
}
