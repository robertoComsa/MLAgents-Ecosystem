using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodPickUp : MonoBehaviour
{
    // VARIABILE

    [SerializeField] private bool needsRandomPosition = false;

    // Pozitia de start
    private Vector3 startingPosition = Vector3.zero;
    // Obiectul (agentul) care ridica/cara aceasta bucata
    private GameObject helper = null;
    // Daca e carat
    private bool isPickedUp = false;
    public bool getIsPickedUp() { return isPickedUp; }

    // METODE 

    private void Awake()
    {
        startingPosition = gameObject.transform.position;
    }

    // Apelata in fiecare frame
    private void Update()
    {
        if (isPickedUp)
        {
            // Pozitia este mereu deasupra agentului helper, cand mancarea este culeasa de acesta
            transform.position = helper.transform.position + new Vector3(0f, 0.7f, 0f);
        }
    }

    // Apelata cand bucata de hrana este atinsa de agentul helper
    public void GetPickedUp(GameObject other)
    {
        helper = other;
        isPickedUp = true;
    }

    // Metoda de resetare
    public void ResetFood()
    {
        ResetPosition();
        isPickedUp = false;
        helper = null;
    }

    // Resetarea pozitiei
    public void ResetPosition()
    {
        if (needsRandomPosition)
        {
            gameObject.transform.position = new Vector3(
                startingPosition.x + Random.Range(Random.Range(-13f, -10f), Random.Range(10f, 13f)),
                startingPosition.y,
                startingPosition.z + Random.Range(Random.Range(-13f, -10f), Random.Range(10f, 13f))
            );
        }
        else gameObject.transform.position = startingPosition;

    }
}
