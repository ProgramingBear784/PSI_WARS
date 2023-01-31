using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PassCommands : MonoBehaviour
{

    // declares game manager
    public Game_Manager gm;

    // areas of play
    public GameObject P1Hand;

    // resource area columns
    public GameObject digitalSplicingColumn;
    public GameObject neurogenesisColumn;
    public GameObject bioAccelerationColumn;
    public GameObject materialAnimationColumn;


    void Awake()
    {
        // assigns the game manager
        gm = GameObject.Find("Game_Manager").GetComponent<Game_Manager>();
    }

    // when the pass button is clicked
    public void OnClick()
    {
        // Draws Resource card

            // select a random index from the resource deck
            int random_resource_deck_index = Random.Range(0, gm.resourceDeckCards.Count);
            
            // creates a corresponding resource card in the player's hand
            GameObject resourceCard = Instantiate(gm.resourceDeckPrefabs[random_resource_deck_index], new Vector2(0, 0), Quaternion.identity);
            resourceCard.transform.SetParent(P1Hand.transform, false);

            // adds the corresponding card data object to the list of card data objects in hand
            resourceCard.name = gm.resourceDeckCards[random_resource_deck_index].Name;
            gm.handCards.Add(gm.resourceDeckCards[random_resource_deck_index]);

            // removes the indexed card from the resource deck
            gm.resourceDeckCards.RemoveAt(random_resource_deck_index);
            gm.resourceDeckPrefabs.RemoveAt(random_resource_deck_index);


        // Draws Battle Card

            // select a random index from the battle deck
            int random_battle_deck_index = Random.Range(0, gm.battleDeckCards.Count);
            
            // creates a corresponding battle card in the player's hand
            GameObject battleCard = Instantiate(gm.battleDeckPrefabs[random_battle_deck_index], new Vector2(0, 0), Quaternion.identity);
            battleCard.transform.SetParent(P1Hand.transform, false);

            // adds the corresponding card data object to the list of card data objects in hand
            gm.handCards.Add(gm.battleDeckCards[random_battle_deck_index]);
            battleCard.name = gm.battleDeckCards[random_battle_deck_index].Name;

            // removes the indexed card from the battle deck
            gm.battleDeckCards.RemoveAt(random_battle_deck_index);
            gm.battleDeckPrefabs.RemoveAt(random_battle_deck_index);


        // Replenish Resources

            // resets the resources available to create with to the amount of resources in play
            gm.greenResourcesAvailable = digitalSplicingColumn.transform.childCount;
            gm.blueResourcesAvailable = neurogenesisColumn.transform.childCount;
            gm.redResourcesAvailable = bioAccelerationColumn.transform.childCount;
            gm.purpleResourcesAvailable = materialAnimationColumn.transform.childCount;

            // makes all the resource lights active
            for (int lightIndex = 0; lightIndex < digitalSplicingColumn.transform.childCount; lightIndex++)
            {
                digitalSplicingColumn.transform.GetChild(lightIndex).GetComponent<Image>().color = new Color32(119, 188, 82, 255);
            }

            for (int lightIndex = 0; lightIndex < neurogenesisColumn.transform.childCount; lightIndex++)
            {
                neurogenesisColumn.transform.GetChild(lightIndex).GetComponent<Image>().color = new Color32(1, 105, 182, 255);
            }

            for (int lightIndex = 0; lightIndex < bioAccelerationColumn.transform.childCount; lightIndex++)
            {
                bioAccelerationColumn.transform.GetChild(lightIndex).GetComponent<Image>().color = new Color32(211, 30, 41, 255);
            }

            for (int lightIndex = 0; lightIndex < materialAnimationColumn.transform.childCount; lightIndex++)
            {
                materialAnimationColumn.transform.GetChild(lightIndex).GetComponent<Image>().color = new Color32(108, 14, 107, 255);
            }
    }
}
