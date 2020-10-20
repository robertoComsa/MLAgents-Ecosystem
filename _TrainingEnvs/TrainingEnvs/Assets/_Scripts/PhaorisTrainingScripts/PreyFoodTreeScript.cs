using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyFoodTreeScript : MonoBehaviour
{
    // --- VARIABILE --- //
    MeshRenderer fruitMesh;

    // --- METODE --- //

    private void Start()
    {
        // Stocam in fruitMesh componenta ce afiseaza fructul
        fruitMesh = GetComponent<MeshRenderer>();

        ChangeTag("preyFoodTree");
    }

    // Schimba tag-ul acestui gameObject
    void ChangeTag(string newTag) => gameObject.tag = newTag;

    // Cand fructul este atins de ciocul pasarii phaoris
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("phaorisBeak") && gameObject.CompareTag("preyFoodTree"))
        {
            StartCoroutine(ActivateFruit());
            StartCoroutine(ChangeTag());
            fruitMesh.enabled = false;                                    
        }

    }

    // Coroutina de activare a fructului
    IEnumerator ActivateFruit()
    {
        yield return new WaitForSeconds(15f);
        fruitMesh.enabled = true;
        ChangeTag("preyFoodTree");
    }

    // Coroutina ce asigura ca informatia ajunge la agent inainte de schimbarea tag-ului
    IEnumerator ChangeTag()
    {
        yield return new WaitForSeconds(1F);
        ChangeTag("Untagged");       // Obiectul ramane enabled , dar nu mai este vizibil , prin urmare vrem ca acesta sa nu mai fie detectat   
                                     // de sistemul de cautare al agentului
    }
}
