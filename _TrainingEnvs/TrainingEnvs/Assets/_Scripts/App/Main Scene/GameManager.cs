using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // ---- VARIABILE VIZIBILE IN EDITOR ---- //

    [Tooltip("GameObjectul canvas")][SerializeField] Canvas canvas = null;

    // ------ ACCESORI ------- //

    public bool CanAgentsRequestDecisions { get; set; } = false;

    // -------- METODE ------- //

    // Apelata in fiecare frame
    private void Update()
    {
        // Activate agents

        if (Input.GetKeyDown(KeyCode.M))
        {
            CanAgentsRequestDecisions = true;
            canvas.gameObject.SetActive(false);
        }
    }
}
