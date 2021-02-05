using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotationForPlant : MonoBehaviour
{
    // Valoarea cu care mutam planta aleatoriu
    [SerializeField] float replacingDistance = 0f;
    [SerializeField] Transform parentTransform = null;
    [SerializeField] float Y_value = 0f;

    // Metode sistem

    // Start is called before the first frame update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ResetPlantPosition();
    }

    // Metode legate de amplasarea copacului

    // Roteste planta pe axa y 
    public void RotateFoodPlant()
    {
        transform.Rotate(new Vector3(0f, Random.Range(0f, 360f), 0f), Space.Self);
    }

    // Plaseaza aleatoriu pe axele x,z
    public void RandomlyPlacePlant()
    {
        gameObject.transform.position = new Vector3(parentTransform.position.x + Random.Range(-replacingDistance, +replacingDistance),
                                                    10f + Random.Range(-Y_value,Y_value),
                                                    parentTransform.position.z + Random.Range(-replacingDistance, +replacingDistance)
                                                    );
    }


    public void ResetPlantPosition()
    {
        RotateFoodPlant();
        RandomlyPlacePlant();
    }
}
