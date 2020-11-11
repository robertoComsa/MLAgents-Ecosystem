using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPositionForHelper : MonoBehaviour
{
    // Pozitiile in care helperul poate fi instantiat
    [Header("Pozitiile de instantiere")] [SerializeField] private Vector3[] positions= null;

    // Resetarea pozitiei
    public void ResetPosition()
    {
        transform.localPosition = positions[Random.Range(0, positions.Length)];
    }

    private void Start()
    {
        ResetPosition();
    }
}
