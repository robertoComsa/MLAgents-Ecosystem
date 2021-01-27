using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AgentParameters
{
    // Parametri infometare (Comuni intre agentii terestri)
    public float starvingInterval;

    // Helios, Phaoris
    public float MoveSpeed;
    public float SearchProximity;

    // Helios, Mulak
    public float RotationSpeed;

    // Mulak;
    public float MateProximity;
    public float DashCooldown;
    public float DashForce;

    // Phaoris
    public float Y_RotationSpeed;
    public float X_RotationSpeed;
    public float DeliveryDistance;
}

