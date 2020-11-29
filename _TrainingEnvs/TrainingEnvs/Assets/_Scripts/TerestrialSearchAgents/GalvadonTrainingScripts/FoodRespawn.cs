using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodRespawn : MonoBehaviour
{
    // Pozitia de start (centrul spatiului de antrenare) 
    private Vector3 startingPosition = Vector3.zero;
    // Distanta pozitiilor de instantiere fata de punctul de start
    [SerializeField] private float distanceToAdd = 0f;

    // Seteaza pozitia initiala inainte de start
    private void Awake()
    {
        startingPosition = gameObject.transform.position;
        //StartCoroutine(Destroy());
    }

    private void Update()
    {
        // Daca simularea s-a incheiat distrugem acest obiect
        if (GameManager.Instance.SimulationEnded)
            Destroy(gameObject);
    }

    // --------------- METODE folosite in antrenamente nu si in scena finala ----- 
    IEnumerator Destroy()
    {
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }

    public void RespawnFood()
    {
        int value = Random.Range(0, 9);
        switch(value)
        {
            case 0:
                gameObject.transform.position = startingPosition;
                gameObject.SetActive(true);
                break;
            case 1:
                gameObject.transform.position = startingPosition + new Vector3(distanceToAdd, 0f, 0f);
                gameObject.SetActive(true);
                break;
            case 2:
                gameObject.transform.position = startingPosition - new Vector3(distanceToAdd, 0f, 0f);
                gameObject.SetActive(true);
                break;
            case 3:
                gameObject.transform.position = startingPosition + new Vector3(0f, 0f, distanceToAdd);
                gameObject.SetActive(true);
                break;
            case 4:
                gameObject.transform.position = startingPosition - new Vector3(0f, 0f, distanceToAdd);
                gameObject.SetActive(true);
                break;
            case 5:
                gameObject.transform.position = startingPosition + new Vector3(distanceToAdd, 0f, distanceToAdd);
                gameObject.SetActive(true);
                break;
            case 6:
                gameObject.transform.position = startingPosition - new Vector3(distanceToAdd, 0f, distanceToAdd);
                gameObject.SetActive(true);
                break;
            case 7:
                gameObject.transform.position = startingPosition + new Vector3(distanceToAdd, 0f, -distanceToAdd);
                gameObject.SetActive(true);
                break;
            case 8:
                gameObject.transform.position = startingPosition - new Vector3(distanceToAdd, 0f, -distanceToAdd);
                gameObject.SetActive(true);
                break;
        }
    }

}
