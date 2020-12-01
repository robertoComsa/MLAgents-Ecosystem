using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AgentParameters
{
    // Parametri infometare 
    public float timeBetweenHungerTicks;
    public float hungerTickValue;
    public float hungerFactor;

    // Helios, Mulak, Galvadon & Phaoris
    public int MoveSpeed;
    public int SearchProximity;

    // Helios, Mulak & Galvadon
    public int RotationSpeed;

    // Mulak;
    public int MateProximity;

    // Phaoris
    public int Y_RotationSpeed;
    public int X_RotationSpeed;
    public int DeliveryDistance;
}

