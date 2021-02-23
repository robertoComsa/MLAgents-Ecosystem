using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatisticsManager : Singleton<StatisticsManager>
{
    // -------------------------------------------------- VARIABILE VIZIBILE IN EDITOR --------------------------------------- //

    //[Tooltip("Daca folosim managerul de statistici")] [SerializeField] private bool useThis = false;
    [Tooltip("Componenta text a statisticilor")] [SerializeField] private Text simData = null;

    // ------------------------------------------------------ METODE --------------------------------------------------------- //

    private void Update()
    {
        if (ReturnAgentsNumber() <= 0 && GameManager.Instance.CanAgentsRequestDecisions == true)
        {
            GameManager.Instance.EndSimulationOnAgentsDeath();
        }
            
    }

    // Returneaza (momentan) numarul agentilor TSA (in caz ca toti mor , incheiem simularea)
    private int ReturnAgentsNumber()
    {
        return HeliosAgentsNumber + MulakAgentsNumber;
    }

    // --------------------------------------------- VARIABILE & METODE GESTIONARE VARIABILE ----------------------------------------------------- //

    // Numarul de agenti Helios
    private int HeliosAgentsNumber = 0;
    public int GetHeliosAgentsNumber() { return HeliosAgentsNumber; }

    // Numarul de agenti Mulak
    private int MulakAgentsNumber = 0;
    public int GetMulakAgentsNumber() { return MulakAgentsNumber; }

    // Numarul de agenti Phaoris
    private int PhaorisAgentsNumber = 0;
    public int GetPhaorisAgentsNumber() { return PhaorisAgentsNumber; }

    // Metoda publica ce gestioneaza numarul de agenti pentru fiecare agenti in parte.
    // action poate fi: set (seteaza numarul) , add (adauga) , remove (scade)
    // agent poate fi: Helios , Mulak.
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
                    case "Phaoris":
                        PhaorisAgentsNumber = value;
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
                    case "Phaoris":
                        PhaorisAgentsNumber += value;
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
                    case "Phaoris":
                        PhaorisAgentsNumber = -value;
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


    // -- METODA MODIFICARE DATE SIMULARE -- //
    public void ModifySimData(string action = "reset") //  Reset va reseta total datele , numele unui parametru va incrementa acel parametru cu 1
    {
        switch (action)
        {
            case "reset":

                // Numarul de agenti
                HeliosAgentsNumber = 0;
                MulakAgentsNumber = 0;
                PhaorisAgentsNumber = 0;

                //
                mulaksCreated = 0;
                mulaksEaten = 0;
                mulakStarved = 0;
                heliosStarved = 0;

                break;

            case "mulaksCreated":

                mulaksCreated++;

                break;

            case "MulakAgentsNumber":

                MulakAgentsNumber++;

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
        }

    }

    // <>--<> SETAREA DATELOR INITIALE <>--<>


    // Numar de agenti Helios initial
    private int initialHeliosNumber = 0;

    // Numar de agenti Mulak initial
    private int initialMulakNumber = 0;

    // Numar de agenti Phaoris initial
    private int initialPhaorisNumber = 0;

    // Metoda de setare a numarului de agenti instantiati
    public void SetInitialAgentNumbers(string action)
    {
        switch (action)
        {
            case "set":

                initialHeliosNumber = HeliosAgentsNumber;
                initialMulakNumber = MulakAgentsNumber;
                initialPhaorisNumber = PhaorisAgentsNumber;

                break;

            case "reset":

                initialHeliosNumber = 0;
                initialMulakNumber = 0;
                initialPhaorisNumber = 0;

                break;
        }     
    }

    // <>--<> SETAREA TEXTULUI LEGAT DE DATELE SIMULARII  <>--<>


    // ------------------ Setarea simData text ------------- //s
    public void SetSimDataTxt()
    {
        simData.text = "Au fost instantiati: \n"
            // Instantierea initiala
            + initialHeliosNumber + " agenti Helios \n"
            + initialMulakNumber + " agenti Mulak \n"
            + initialPhaorisNumber + " agenti Phaoris \n\n"

            // Mulak (mancati / nascuti)
            + mulaksCreated + " agenti Mulak au fost creati (prin multiplicare) \n"
            + mulaksEaten + " angenti Mulak au fost mancati \n\n"

            // Decese din cauza infometarii
            + "Au murit de foame: \n"
            + heliosStarved + " agenti Helios\n"
            + mulakStarved + " agenti Mulak\n\n"

            // Durata simularii
            + "Durata simularii (secunde): " + (GameManager.Instance.GetSimDuration()+1).ToString("F2");
    }
}
