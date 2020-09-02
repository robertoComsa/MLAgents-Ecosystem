using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MulakTesting : MonoBehaviour
{


    // Pozitia de start (folosita in reasezarea agentului in scena)
    private Vector3 startingPosition = Vector3.zero;
    // Componenta rigidBody (ne permita sa aplicam manevre fizice)
    private Rigidbody rb;

    // Verifica daca agentul este pe pamant
   // private bool isGrounded = true;

    // Verifica daca agentul poate fi resetat
   // private bool isDone = false;

    // Verifica daca agentul s-a imperecheat deja
    [SerializeField] private bool isMated = false;
    // accesor al variabilei isMated 

    public bool GetIsMated()
    {
        return isMated;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        rb.inertiaTensorRotation = Quaternion.identity;
    }

    public void GetMated()
    {
        rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
        //isGrounded = false;
        //isDone = true;
        isMated = true;
    }


}
