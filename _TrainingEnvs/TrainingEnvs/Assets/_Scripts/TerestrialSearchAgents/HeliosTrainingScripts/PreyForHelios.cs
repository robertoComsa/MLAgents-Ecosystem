using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyForHelios : MonoBehaviour
{
    // VARIABILE

    [SerializeField] private bool needsReset = false;

    // Pozitia de start (folosita in reasezarea agentului in scena)
    private Vector3 startingPosition = Vector3.zero;

    // METODE

    private void Awake()
    {
        startingPosition = gameObject.transform.position;
        if(needsReset) PlaceRandomly();
    }

    // Plaseaza prada aleatoriu in spatiul de antrenare. (Atat pozitie cat si rotatie)
    public void PlaceRandomly()
    {
        transform.position = new Vector3(
            startingPosition.x + Random.Range(Random.Range(-15f,-12f), Random.Range(12f, 15f)), 
            startingPosition.y, 
            startingPosition.z + Random.Range(Random.Range(-15f, -12f), Random.Range(12f, 15f))
            );
        Quaternion newRotation = Quaternion.Euler(transform.rotation.x, Random.Range(0f, 360f), transform.rotation.z);
        transform.rotation = newRotation;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("predator"))
            PlaceRandomly();
       // if (other.gameObject.CompareTag("helper") && other.gameObject.GetComponent<GalvadonAgentFeed>().GetCarryingFood() == true)
        //    PlaceRandomly();
    }
}
