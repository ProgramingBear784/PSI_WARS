using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragDropFighters : MonoBehaviour
{

    // declares the game manager
    public Game_Manager gm;
    
    // areas on the screen
    private GameObject Canvas;
    private GameObject dropZone;

    // declares correct area for the card to be placed
    private GameObject DropZone;

    // makes variables for where the card was before dragging
    private GameObject startParent;
    private Vector2 startPosition;

    // declares bools for if the card is dragging and if it is over the correct placeable area on the screen
    private bool isOverDropZone;
    private bool isDragging = false;

    // declares the resource areas
    private GameObject digitalSplicingColumn;
    private GameObject neurogenesisColumn;
    private GameObject bioAccelerationColumn;
    private GameObject materialAnimationColumn;

    // on start
    void Awake()
    {

        // assigns the game manager
        gm = GameObject.Find("Game_Manager").GetComponent<Game_Manager>();

        // assigns the main canvas
        Canvas = GameObject.Find("Main Canvas");

        // assigns the correct area the card can be placed
        DropZone = GameObject.Find("P1Fighters_Area");

        // assigns the correct resource areas
        digitalSplicingColumn = GameObject.Find("digitalSplicingColumn");
        neurogenesisColumn = GameObject.Find("neurogenesisColumn");
        bioAccelerationColumn = GameObject.Find("bioAccelerationColumn");
        materialAnimationColumn = GameObject.Find("materialAnimationColumn");
    }

    // when the card is over detectable area
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // assigns the area the card is over to the object the card came in contact with
        dropZone = collision.gameObject;

        // detects if the area the card is over is the correct placeable area
        if (dropZone == DropZone)
        {   
            isOverDropZone = true;
        }
    }

    // when card leaves detectable area
    private void OnCollisionExit2D(Collision2D collision)
    {
        // not over the correct placeable area
        isOverDropZone = false;

        // not over detectable area
        dropZone = null;
    }

    // player starts dragging the card
    public void StartDrag()
    {
        // the player is marked as dragging
        isDragging = true;

        // retrieves the original position and parent of the card
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
    }

    // player stops dragging
    public void EndDrag()
    {

        // player is no longer marked as dragging
        isDragging = false;

        // if the card is placed in legal zone
        if ((isOverDropZone) && (dropZone == DropZone))
        {

            // retrieves the corresponding data for the card that was dragged and dropped
            int index = 0;
            for (int i = 0; i < gm.handCards.Count; i++)
            {
                if (gameObject.name == gm.handCards[i].Name)
                {
                    index = i;
                }
            }
            
            // if the player has enough available resources to play the card
            if ((gm.greenResourcesAvailable - gm.handCards[index].GreenCost >= 0) 
            && (gm.blueResourcesAvailable - gm.handCards[index].BlueCost >= 0) 
            && (gm.redResourcesAvailable - gm.handCards[index].RedCost >= 0) 
            && (gm.purpleResourcesAvailable - gm.handCards[index].PurpleCost >= 0))
            {

                // the parent of the card is changed to the area the card was placed
                transform.SetParent(dropZone.transform, false);

                // resassigns the amount of available green resources and turns of the corresponding number of resource lights
                gm.greenResourcesAvailable -= gm.handCards[index].GreenCost;
                for (int lastActiveGreenLightIndex = 0; lastActiveGreenLightIndex < (gm.totalGreenResources - gm.greenResourcesAvailable); lastActiveGreenLightIndex++)
                {
                    digitalSplicingColumn.transform.GetChild(lastActiveGreenLightIndex).GetComponent<Image>().color = new Color32(63, 82, 63, 255);
                }

                // resassigns the amount of available blue resources and turns of the corresponding number of resource lights
                gm.blueResourcesAvailable -= gm.handCards[index].BlueCost;
                for (int lastActiveBlueLightIndex = 0; lastActiveBlueLightIndex < (gm. totalBlueResources - gm.blueResourcesAvailable); lastActiveBlueLightIndex++)
                {
                    neurogenesisColumn.transform.GetChild(lastActiveBlueLightIndex).GetComponent<Image>().color = new Color32(0, 75, 88, 255);
                }

                // resassigns the amount of available red resources and turns of the corresponding number of resource lights
                gm.redResourcesAvailable -= gm.handCards[index].RedCost;
                for (int lastActiveRedLightIndex = 0; lastActiveRedLightIndex < (gm.totalRedResources - gm.redResourcesAvailable); lastActiveRedLightIndex++)
                {
                    bioAccelerationColumn.transform.GetChild(lastActiveRedLightIndex).GetComponent<Image>().color = new Color32(78, 38, 32, 255);
                }

                // resassigns the amount of available purple resources and turns of the corresponding number of resource lights
                gm.purpleResourcesAvailable -= gm.handCards[index].PurpleCost;
                for (int lastActivePurpleLightIndex = 0; lastActivePurpleLightIndex < (gm.totalPurpleResources - gm.purpleResourcesAvailable); lastActivePurpleLightIndex++)
                {
                    materialAnimationColumn.transform.GetChild(lastActivePurpleLightIndex).GetComponent<Image>().color = new Color32(59, 38, 66, 255);
                }
            
                // removes the corresponding card from the player's hand
                gm.handCards.RemoveAt(index);

            }
            // if the player placed the card in legal zone but did not have enough resources to play it
            else
            {
                // sets the cards position and parent to where it was
                transform.position = startPosition;
                transform.SetParent(startParent.transform, false);

                // pop-up saying the player does not have enough resources
                Debug.Log("NOT ENOUGH CREATION UNITS");
            }

        }
        // the player dropped the card in illegal zone 
        else
        {
            // sets the cards position and parent to where it was
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
        }
    }
    
    // moves the card to the player's mouse if the player is dragging
    void Update()
    {
        // test if the player is dragging the card
        if (isDragging)
        {
            // sets the card position to the player's mouse
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            transform.SetParent(Canvas.transform, true);
        }
    }
}