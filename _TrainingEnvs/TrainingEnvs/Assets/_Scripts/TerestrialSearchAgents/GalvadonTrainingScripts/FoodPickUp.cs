using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodPickUp : MonoBehaviour
{
    // VARIABILE VIZIBILE

    [SerializeField] private bool needsRandomPosition = false;
    [SerializeField] float destroyTimeAfterDrop = 0f;

    // VARIABILE

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
        // Daca simularea s-a incheiat distrugem acest obiect
        if (GameManager.Instance.SimulationEnded)
            Destroy(gameObject);

        // Asigura efectul de *picked up* 
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
   
    // Sistem ce face ca fructul sa dispara la x timp dupa ce a atins pamantul 

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
           StartCoroutine(DestroyFruit(destroyTimeAfterDrop));
    }

    IEnumerator DestroyFruit(float timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);
        if(isPickedUp==false)
            Destroy(gameObject);
    }

    // -------------------------------  Metode resetare - Folosite in antrenamente nu si in scena finala

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
