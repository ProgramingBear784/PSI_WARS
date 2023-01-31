using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game_Manager : MonoBehaviour
{

    // card prefabs

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
            // card name
            public string Name;

            // card type
            public string Type;

            // card costs
            public int GreenCost;
            public int BlueCost;
            public int RedCost;
            public int PurpleCost;
            public int BurnCost;

            // card stats
            public string CyberStat;
            public string PsionicStat;
            public string PhysicalStat;

            // card description
            public string Description;

            // templete for making card
            public card(string cardName, string cardType, 
            int cardGreenCost, int cardBlueCost, int cardRedCost, int cardPurpleCost, int cardBurnCost,
            string cardCyberStat, string cardPsionicStat, string cardPhysicalStat,
            string cardDescription)
            {
                // card name
                Name = cardName;

                // card type
                Type = cardType;

                // card costs
                GreenCost = cardGreenCost;
                BlueCost = cardBlueCost;
                RedCost = cardRedCost;
                PurpleCost = cardPurpleCost;
                BurnCost = cardBurnCost;

                // card stats
                CyberStat = cardCyberStat;
                PsionicStat = cardPsionicStat;
                PhysicalStat = cardPhysicalStat;

                // card description
                Description = cardDescription;
            }
        }
    
    // resource deck info storage

        // declare list for resource deck, where each item has build in information on the card
        public List<card> resourceDeckCards = new List<card>();

        // declare list for resource deck, where each item is the prefab for the card
        public List<GameObject> resourceDeckPrefabs = new List<GameObject>();


    // battle deck info storage

        // declare list for battle deck, where each item has build in information on the card
        public List<card> battleDeckCards = new List<card>();

        // declare list for battle deck, where each item is the prefab for the card
        public List<GameObject> battleDeckPrefabs = new List<GameObject>();


    // hand info storage

        // declare list for player's hand, where each item has build in information on the card
        public List<card> handCards = new List<card>();


    // resource area info storage

        // declare list for player's resource area, where each item has build in information on the card
        public List<card> resourceAreaCards = new List<card>();


    // battle unit area info storage

        // declare list for player's battle unit area, where each item has build in information on the card
        public List<card> battleUnitAreaCards = new List<card>();

    
    // resource trackers

        // total number of given creation unit type in play
        // total number of given creation unit type that available for use

        public int totalGreenResources;
        public int greenResourcesAvailable;
        
        public int totalBlueResources;
        public int blueResourcesAvailable;

        public int totalRedResources;
        public int redResourcesAvailable;

        public int totalPurpleResources;
        public int purpleResourcesAvailable;

    // on game start
    void Start()
    {
        
        // reset resources
        
            // total number of given creation unit type in play
            // total number of given creation unit type that available for use

            totalGreenResources = 0;
            greenResourcesAvailable = 0;
            
            totalBlueResources = 0;
            blueResourcesAvailable = 0;

            totalRedResources = 0;
            redResourcesAvailable = 0;

            totalPurpleResources = 0;
            purpleResourcesAvailable = 0;


        // creates resource deck

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


        // creates battle deck

            for (int i = 1; i <= 8; i++)
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