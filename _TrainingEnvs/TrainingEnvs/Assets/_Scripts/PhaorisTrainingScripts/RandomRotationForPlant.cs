using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotationForPlant : MonoBehaviour
{
    // Roteste planta pe axa y 
    public void RotateFoodPlant()
    {
        transform.Rotate(new Vector3(0f,Random.Range(0f,360f),0f), Space.Self);
    }

    // Start is called before the first frame update
    void Start()
    {
        RotateFoodPlant();
    }
}
