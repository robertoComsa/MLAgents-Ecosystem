using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyFoodTreeScript : MonoBehaviour
{
    // Cand fructul este atins de ciocul pasarii phaoris
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("phaorisBeak"))
        {
            gameObject.SetActive(false);
            StartCoroutine(ActivateFruit());
        }

    }

    // Coroutina de activare a fructului
    IEnumerator ActivateFruit()
    {
        yield return new WaitForSeconds(15f);
        gameObject.SetActive(true);
    }


}
