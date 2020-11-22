using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // ---- VARIABILE VIZIBILE IN EDITOR ---- //

    [Tooltip("GameObjectul canvas")][SerializeField] Canvas canvas = null;

    // -------- VARIABILE ------- //

    // Transformul actionbar-ului (element GUI)
    Transform actionBar = null;

    // ------ PROPRIETATI ------- //

    public bool CanAgentsRequestDecisions { get; set; } = false;

    // --------------------------------------------------------------- METODE SISTEM ------------------------------------------------------------------- //

    // Prima metoda apelata ( o singura data )
    protected override void Awake()
    {
        base.Awake();
        actionBar = canvas.transform.Find("Actionbar");
    }

    // Apelata in fiecare frame
    private void Update()
    {
        // Activate agents
        if (Input.GetKeyDown(KeyCode.M))
        {
            CanAgentsRequestDecisions = true;
            EnableOrDisableGUI(actionBar, false);
        }
    }

    // --------------------------------------------------------------------- METODE ------------------------------------------------------------------- //
    
    // Metoda folosita la activarea / dezactivarea elementelor GUI din scena
    private void EnableOrDisableGUI(Transform GUIelement , bool state)
    {
        GUIelement.gameObject.SetActive(state);
    }

}
