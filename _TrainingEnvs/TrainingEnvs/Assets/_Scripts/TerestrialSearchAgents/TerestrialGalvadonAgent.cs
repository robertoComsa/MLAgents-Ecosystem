﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerestrialGalvadonAgent : TerestrialSearchAgent
{
    //  ---------------------------------------------------------- VARIABILE ---------------------------------------------------- //

    // int care reprezinta targetul pe care il cautam ( 0 - mancare pentru agent , 1 - mancare pentru erbivori , 2 - erbivori )
    protected int target = 1;
    // Folosit pentru a apela reinstantierea hranei in scena 
    protected FoodPickUp preyFood = null;
    // Utilizata la vizualizarea celui mai apropriat obiect tinta
    protected Color rayColor = Color.magenta;

    // Verificam daca agentul cara hrana
    protected bool carryingFood = false;
    public bool GetCarryingFood() { return carryingFood; }

    // ------------------------------------------------- METODE (Mostenite din) AGENT ------------------------------------------ //

    // Initializarea agentului; apelata o singura data 
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        target = 1;
    }

    // Observatiile numerice oferite agentului
    public override void CollectObservations()
    {
        base.CollectObservations();
        // O valoare int ce poate fi 0 sau 1 si actioneaza ca un boolean dar este int pentru ca vrem sa il oferim ca observatie numerica
        AddVectorObs(target); // 1 valoare int

        // Total: 11 + Observatiile de tip raycast
    }

    // Reseteaza agentul (MaxStep = 0 = infinit -> nu va mai folosi resetare)
    public override void AgentReset()
    {
        base.AgentReset();
        if (preyFood != null) preyFood.ResetFood();
        target = 1;
        carryingFood = false;
    }

    // ----------------------------------------- METODE (Mostenite din) TERESTRIAL SEARCH AGENT ------------------------------------- //

    protected override void FixedUpdate()
    {
        OptimizedCheckInRadius(rayColor);
    }

    /// <summary>
    /// Verifica daca exista tinte intr-un radius setat in jurul agentului
    /// Daca da , colecteaza pentru observatii distanta pana la acesta si pozitia sa
    /// </summary>
    protected override void CheckTargetInProximity()
    {
        ManageTargetTag();

        GameObject[] targets = GameObject.FindGameObjectsWithTag(tagName);

        float nearestDistance = Mathf.Infinity;
        GameObject closestTarget = null;
        bool targetInRadius = false;

        foreach (GameObject actualTarget in targets)
        {
            float distance = Vector3.Distance(transform.position, actualTarget.transform.position);

            if (distance < nearestDistance && distance < searchProximity) // fara a 2-a conditie ar primi distanta fata de cel mai apropriat pradator din toata scena
            {
                switch (target)
                {
                    case 0:
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = actualTarget;
                        break;
                    case 1:
                        if (actualTarget.GetComponent<FoodPickUp>().getIsPickedUp() == false) // Altfel exista cazuri in care cea mai apropriata bucata                                                                                             
                        {                                                                     // este deja carata de alt agent 
                            targetInRadius = true;
                            nearestDistance = distance;
                            closestTarget = actualTarget;
                        }
                        break;
                    case 2:
                        targetInRadius = true;
                        nearestDistance = distance;
                        closestTarget = actualTarget;
                        break;
                }
            }
        }
        if (targetInRadius)
        {
            closestTargetPosition = closestTarget.transform.localPosition;
            distanceToClosestTarget = nearestDistance;
            targetedRayPos = closestTarget.transform.position;
        }
        else
        {
            closestTargetPosition = Vector3.zero;
            distanceToClosestTarget = 0f;
            targetedRayPos = Vector3.zero;
        }
    }

    // ------------------------------------------------------------ METODE GALVADON ---------------------------------------------------- //

    // Seteaza tag-ul si culoarea razei pentru functia de cautare pe baza tintei
    private void ManageTargetTag()
    {
        switch (target)
        {
            case 0:
                tagName = "galvadonFood";
                rayColor = Color.magenta;
                break;
            case 1:
                tagName = "preyFood";
                rayColor = Color.cyan;
                break;
            case 2:
                tagName = "prey";
                rayColor = Color.green;
                break;
        }
    }

    // Logica pentru coliziunea cu mancare (ambele tipuri)
    private void OnTriggerEnter(Collider other)
    {
        // Daca ne lovim de margini
        if (other.CompareTag("boundary"))
        {
            AgentReset();
        }

        // Daca ne lovim de mancarea agentului
        if (other.gameObject.CompareTag("galvadonFood") && target == 0)
        {
            Destroy(other.gameObject);
            target = 1;
        }

        // Daca ne lovim de mancarea erbivorilor 
        if (other.gameObject.CompareTag("preyFood") && carryingFood == false && target == 1)
        {
            preyFood = other.gameObject.GetComponent<FoodPickUp>();
            preyFood.GetPickedUp(gameObject);
            carryingFood = true;
            target = 2;
        }
    }

    // Coliziunea cu obiecte din scena 
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("prey") && carryingFood == true && target == 2)
        {
            // In viitor -  Detroy(preyFood.gameObject);
            preyFood.ResetFood();
            StartCoroutine(WaitToGiveFood());
            target = 0;
            AddReward(0.5f);
        }
    }

    // Existau cazuri in care carryingFood devenea fals inainte ca erbivorul sa primeasca informatia , si astfel erbivorul nu mai incepea rutina de a crea floare
    IEnumerator WaitToGiveFood()
    {
        yield return new WaitForSeconds(0.1f);
        carryingFood = false;
    }
}
