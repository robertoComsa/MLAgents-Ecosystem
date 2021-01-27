using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AgentParameters
{
    // Parametri infometare (Comuni intre agentii terestri)
    public float timeBetweenHungerTicks;
    public float hungerTickValue;
    public float hungerFactor;

    // Helios, Phaoris
    public int MoveSpeed;
    public int SearchProximity;

    // Helios, Mulak
    public int RotationSpeed;

    // Mulak;
    public int MateProximity;
    public float DashCooldown;
    public float DashForce;

    // Phaoris
    public int Y_RotationSpeed;
    public int X_RotationSpeed;
    public int DeliveryDistance;
}

