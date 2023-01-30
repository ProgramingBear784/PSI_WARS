using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game_Manager : MonoBehaviour
{
    // creation units
    public GameObject RedResource;
    public GameObject BlueResource;
    public GameObject GreenResource;
    public GameObject PurpleResource;

    // commanders
    public GameObject sianaCreatureCommander;
    public GameObject daviliamRobotCommander;

    // robots
    public GameObject hydraBot;
    public GameObject laquidorStalker;
    public GameObject therabot;

    // beings
    public GameObject jingwaPsiAvenger;
    public GameObject trangareyScythe;
    public GameObject lifeSnatcher;

    // cyborgs
    public GameObject bladeMaster;
    public GameObject lodnerPredator;
    public GameObject zomberMercenary;

    // equipments
    public GameObject cyberOnslaught;
    public GameObject veqianCyberDefense;
    public GameObject mentalShield;
    public GameObject psionicBolster;
    public GameObject intelliborgFusion;
    public GameObject kranthardArmour;

    // powers
    public GameObject beingRegenerate;
    public GameObject equipmentHunter;
    public GameObject equipmentMechanic;
    public GameObject grankorStasisField;
    public GameObject highEnergyPulse;

    // card creator
    public struct card 
    {
        public string Name;
        public string Type;

        public int GreenCost;
        public int BlueCost;
        public int RedCost;
        public int PurpleCost;
        public int BurnCost;

        public string CyberStat;
        public string PsionicStat;
        public string PhysicalStat;

        public string Description;

        public card(string cardName, string cardType, 
        int cardGreenCost, int cardBlueCost, int cardRedCost, int cardPurpleCost, int cardBurnCost,
        string cardCyberStat, string cardPsionicStat, string cardPhysicalStat,
        string cardDescription)

        {
            Name = cardName;
            Type = cardType;

            GreenCost = cardGreenCost;
            BlueCost = cardBlueCost;
            RedCost = cardRedCost;
            PurpleCost = cardPurpleCost;
            BurnCost = cardBurnCost;

            CyberStat = cardCyberStat;
            PsionicStat = cardPsionicStat;
            PhysicalStat = cardPhysicalStat;

            Description = cardDescription;
        }

    }
    

    public List<card> resourceDeckCards = new List<card>();
    public List<GameObject> resourceDeckPrefabs = new List<GameObject>();

    public List<card> battleDeckCards = new List<card>();
    public List<GameObject> battleDeckPrefabs = new List<GameObject>();

    public static List<card> handCards = new List<card>();

    public static int totalGreenResources;
    public static int greenResourcesAvailable;
    
    public static int totalBlueResources;
    public static int blueResourcesAvailable;

    public static int totalRedResources;
    public static int redResourcesAvailable;

    public static int totalPurpleResources;
    public static int purpleResourcesAvailable;


    void Start()
    {
        
        totalGreenResources = 0;
        greenResourcesAvailable = 0;
        
        totalBlueResources = 0;
        blueResourcesAvailable = 0;

        totalRedResources = 0;
        redResourcesAvailable = 0;

        totalPurpleResources = 0;
        purpleResourcesAvailable = 0;

        for (int i = 1; i <= 8; i++)
        {
            card redCreationUnitCard = new card("bioAcceleration" + i, "creationCard_creationUnit", -1, -1, -1, -1, -1, "NAN", "NAN", "NAN", "...");
            resourceDeckCards.Add(redCreationUnitCard);
            resourceDeckPrefabs.Add(RedResource);

            card blueCreationUnitCard = new card("neurogenesis" + i, "creationCard_creationUnit", -1, -1, -1, -1, -1, "NAN", "NAN", "NAN", "...");
            resourceDeckCards.Add(blueCreationUnitCard);
            resourceDeckPrefabs.Add(BlueResource);

            card greenCreationCard = new card("digitalSplicing" + i, "creationCard_creationUnit", -1, -1, -1, -1, -1, "NAN", "NAN", "NAN", "...");
            resourceDeckCards.Add(greenCreationCard);
            resourceDeckPrefabs.Add(GreenResource);

            card purpleCreationCard = new card("materialAnimation" + i, "creationCard_creationUnit", -1, -1, -1, -1, -1, "NAN", "NAN", "NAN", "...");
            resourceDeckCards.Add(purpleCreationCard);
            resourceDeckPrefabs.Add(PurpleResource);
        }

        for (int i = 1; i <= 3; i++)
        {
            card bladeMasterCard = new card("bladeMaster" + i, "battleCard_battleUnit_fighter_cryborg", 2, 1, 1, 1, 0, "2", "2", "2", "...");
            battleDeckCards.Add(bladeMasterCard);
            battleDeckPrefabs.Add(bladeMaster);

            card hydraBotCard = new card("hydraBot" + i, "battleCard_battleUnit_fighter_robot", 1, 0, 0, 1, 0, "X", "NAN", "1", "...");
            battleDeckCards.Add(hydraBotCard);
            battleDeckPrefabs.Add(hydraBot);

            card jingwaPsiAvengerCard = new card("jingwaPsiAvenger" + i, "battleCard_battleUnit_fighter_being", 0, 2, 1, 0, 0, "0", "3", "1", "...");
            battleDeckCards.Add(jingwaPsiAvengerCard);
            battleDeckPrefabs.Add(jingwaPsiAvenger);
        }
    }
}