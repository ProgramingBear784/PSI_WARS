using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassCommands : MonoBehaviour
{

    
    public GameObject P1Hand;
    public Game_Manager gm;

    public GameObject digitalSplicingColumn;
    public GameObject neurogenesisColumn;
    public GameObject bioAccelerationColumn;
    public GameObject materialAnimationColumn;

    public void OnClick()
    {
        int random_resource_deck_index = Random.Range(0, gm.resourceDeckCards.Count);
        
        GameObject resourceCard = Instantiate(gm.resourceDeckPrefabs[random_resource_deck_index], new Vector2(0, 0), Quaternion.identity);
        resourceCard.name = gm.resourceDeckCards[random_resource_deck_index].Name;
        Game_Manager.handCards.Add(gm.resourceDeckCards[random_resource_deck_index]);
        gm.resourceDeckCards.RemoveAt(random_resource_deck_index);
        gm.resourceDeckPrefabs.RemoveAt(random_resource_deck_index);
        resourceCard.transform.SetParent(P1Hand.transform, false);

        int random_battle_deck_index = Random.Range(0, gm.battleDeckCards.Count);
        
        GameObject battleCard = Instantiate(gm.battleDeckPrefabs[random_battle_deck_index], new Vector2(0, 0), Quaternion.identity);
        battleCard.name = gm.battleDeckCards[random_battle_deck_index].Name;
        Game_Manager.handCards.Add(gm.battleDeckCards[random_battle_deck_index]);
        gm.battleDeckCards.RemoveAt(random_battle_deck_index);
        gm.battleDeckPrefabs.RemoveAt(random_battle_deck_index);
        battleCard.transform.SetParent(P1Hand.transform, false);

        Game_Manager.greenResourcesAvailable = digitalSplicingColumn.transform.childCount;
        Game_Manager.blueResourcesAvailable = neurogenesisColumn.transform.childCount;
        Game_Manager.redResourcesAvailable = bioAccelerationColumn.transform.childCount;
        Game_Manager.purpleResourcesAvailable = materialAnimationColumn.transform.childCount;

    }
}
