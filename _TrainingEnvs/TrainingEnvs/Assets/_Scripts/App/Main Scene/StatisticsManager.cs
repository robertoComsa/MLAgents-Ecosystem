using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatisticsManager : Singleton<StatisticsManager>
{
    // -------------------------------------------------- VARIABILE VIZIBILE IN EDITOR --------------------------------------- //

    //[Tooltip("Daca folosim managerul de statistici")] [SerializeField] private bool useThis = false;

    // --------------------------------------------- VARIABILE & METODE GESTIONARE VARIABILE ----------------------------------------------------- //

    // Numarul de agenti Helios
    private int HeliosAgentsNumber = 0;
    public int GetHeliosAgentsNumber() { return HeliosAgentsNumber; }

    // Numarul de agenti Mulak
    private int MulakAgentsNumber = 0;
    public int GetMulakAgentsNumber() { return MulakAgentsNumber; }

    // Numarul de agenti Galvadon
    private int GalvadonAgentsNumber = 0;
    public int GetGalvadonAgentsNumber() { return GalvadonAgentsNumber; }

    // Metoda publica ce gestioneaza numarul de agenti pentru fiecare agenti in parte.
    // action poate fi: set (seteaza numarul) , add (adauga) , remove (scade)
    // agent poate fi: Helios , Mulak , Galvadon
    public void ModifyAgentsNumber(string action , string agent , int value = 0) // Daca actiunea este set , fara a oferi o valoare actioneaza ca un reset!
    {
        switch (action)
        {
            case "set":

                switch (agent)
                {
                    case "Helios":
                        HeliosAgentsNumber = value;
                        break;
                    case "Mulak":
                        MulakAgentsNumber = value;
                        break;
                    case "Galvadon":
                        GalvadonAgentsNumber = value;
                        break;
                }
                
                break;

            case "add":

                if (value == 0) value = 1;

                switch (agent)
                {
                    case "Helios":
                        HeliosAgentsNumber += value;
                        break;
                    case "Mulak":
                        MulakAgentsNumber += value;
                        break;
                    case "Galvadon":
                        GalvadonAgentsNumber += value;
                        break;
                }

                break;

            case "remove":

                if (value == 0) value = 1;

                switch (agent)
                {
                    case "Helios":
                        HeliosAgentsNumber -= value;
                        break;
                    case "Mulak":
                        MulakAgentsNumber -= value;
                        break;
                    case "Galvadon":
                        GalvadonAgentsNumber -= value;
                        break;
                }

                break;
        }

    }

    // -------- DATE SIMULARE -------- //

    // Numarul de agenti Mulak creati (prin multiplicare)
    private int mulaksCreated = 0;

    // Numarul de agenti Mulak mancati
    private int mulaksEaten = 0;

    // Numarul de agenti Mulak ce au murit de foame 
    private int mulakStarved = 0;

    // Numarul de agenti Helios ce au murit de foame
    private int heliosStarved = 0;

    // Numarul de agenti Galvadon ce au murit de foame
    private int galvadonStarved = 0;

    // -- METODA MODIFICARE DATE SIMULARE -- //
    public void ModifySimData(string action = "reset") //  Reset va reseta total datele , numele unui parametru va incrementa acel parametru cu 1
    {
        switch (action)
        {
            case "reset":

                mulaksCreated = 0;
                mulaksEaten = 0;
                mulakStarved = 0;
                heliosStarved = 0;
                galvadonStarved = 0;

                break;

            case "mulaksCreated":

                mulaksCreated++;

                break;

            case "mulaksEaten":

                mulaksEaten++;

                break;

            case "mulakStarved":

                mulakStarved++;

                break;

            case "heliosStarved":

                heliosStarved++;

                break;

            case "galvadonStarved":

                galvadonStarved++;

                break;
        }

    }

    // Numar de agenti Helios initial
    private int initialHeliosNumber = 0;

    // Numar de agenti Helios initial
    private int initialMulakNumber = 0;

    // Numar de agenti Helios initial
    private int initialGalvadonNumber = 0;

    // Metoda de setare a numarului de agenti instantiati
    public void SetInitialAgentNumbers()
    {
        initialHeliosNumber = HeliosAgentsNumber;
        initialMulakNumber = MulakAgentsNumber;
        initialGalvadonNumber = GalvadonAgentsNumber;
    }
}
