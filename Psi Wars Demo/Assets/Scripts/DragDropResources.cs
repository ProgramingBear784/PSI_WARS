using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragDropResources : MonoBehaviour
{

    // declares the game manager
    public Game_Manager gm;

    // sets prefabs for resource lights
    public GameObject digitalSplicingResourceLight;
    public GameObject neurogenesisResourceLight;
    public GameObject bioAccelerationResourceLight;
    public GameObject materialAnimationResourceLight;

    // declares the resource area columns
    private GameObject digitalSplicingColumn;
    private GameObject neurogenesisColumn;
    private GameObject bioAccelerationColumn;
    private GameObject materialAnimationColumn;

    // declares the main canvas
    private GameObject Canvas;

    // declares the legal dropzone
    private GameObject DropZone;

    // declares the card start parent and position
    private GameObject startParent;
    private Vector2 startPosition;

    // declares any detectable area the card comes into contact with
    private GameObject dropZone;

    // declares bool if the card is over a detectable area
    private bool isOverDropZone;

    // declares bool if the card is being dragged, sets to false
    private bool isDragging = false;
    

    // when the card is activated
    void Awake()
    {

        // assigns the game manager
        gm = GameObject.Find("Game_Manager").GetComponent<Game_Manager>();

        // assigns the main canvas
        Canvas = GameObject.Find("Main Canvas");

        // assigns the resource area
        DropZone = GameObject.Find("P1Resource_Area");

        // assigns the resource area columns
        digitalSplicingColumn = GameObject.Find("digitalSplicingColumn");
        neurogenesisColumn = GameObject.Find("neurogenesisColumn");
        bioAccelerationColumn = GameObject.Find("bioAccelerationColumn");
        materialAnimationColumn = GameObject.Find("materialAnimationColumn");

    }


    // when the card starts being dragged
    public void StartDrag()
    {
        // marks that the card is being dragged and records the cards start position and parent
        isDragging = true;
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
    }


    // when the card enters a detectable area
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // current area is marked and checks if that is the legal dropzone
        dropZone = collision.gameObject;
        if (dropZone == DropZone)
        {   
            isOverDropZone = true;
        }
    }


    // when card leaves a detectable area
    private void OnCollisionExit2D(Collision2D collision)
    {
        // card is not over legal dropzone
        isOverDropZone = false;
        dropZone = null;
    }


    // when the card is dropped
    public void EndDrag()
    {
        // the card is marked as not being dragged
        isDragging = false;

        // if the card is over a legal dropzone
        if ((isOverDropZone) && (dropZone == DropZone))
        {

            // gets the card corresponding index for the list of the data about the cards in the player's hand
            int index = 0;
            for (int i = 0; i < gm.handCards.Count; i++){
                if (gameObject.name == gm.handCards[i].Name)
                {
                    index = i;
                }
            } 


            // adds one of the corresponding creation unit to the corresponding resource area column

                if (gameObject.transform.name.Contains("digitalSplicing"))
                {
                    GameObject greenResourceLight = Instantiate<GameObject>(digitalSplicingResourceLight, new Vector2(0, 0), Quaternion.identity);
                    greenResourceLight.transform.SetParent(digitalSplicingColumn.transform, false);
                    gm.totalGreenResources = digitalSplicingColumn.transform.childCount;
                    gm.greenResourcesAvailable += 1;
                } 
                else if (gameObject.transform.name.Contains("neurogenesis"))
                {
                    GameObject blueResourceLight = Instantiate<GameObject>(neurogenesisResourceLight, new Vector2(0, 0), Quaternion.identity);
                    blueResourceLight.transform.SetParent(neurogenesisColumn.transform, false);
                    gm.totalBlueResources = neurogenesisColumn.transform.childCount;
                    gm.blueResourcesAvailable += 1;
                }
                else if (gameObject.transform.name.Contains("bioAcceleration"))
                {
                    GameObject redResourceLight = Instantiate<GameObject>(bioAccelerationResourceLight, new Vector2(0, 0), Quaternion.identity);
                    redResourceLight.transform.SetParent(bioAccelerationColumn.transform, false);
                    gm.totalRedResources = bioAccelerationColumn.transform.childCount;
                    gm.redResourcesAvailable += 1;
                }
                else if (gameObject.transform.name.Contains("materialAnimation"))
                {
                    GameObject purpleResourceLight = Instantiate<GameObject>(materialAnimationResourceLight, new Vector2(0, 0), Quaternion.identity);
                    purpleResourceLight.transform.SetParent(materialAnimationColumn.transform, false);
                    gm.totalPurpleResources = materialAnimationColumn.transform.childCount;
                    gm.purpleResourcesAvailable += 1;
                }

            // removes the card from the scene
            gameObject.SetActive(false);

            // removes the corresponding information about the card from the list of cards in the player's hand
            gm.handCards.RemoveAt(index);

        }
        // card dropped in illegal zone 
        else
        {
            // sets the card's parent and position to where it was
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
        }
    }
    

    void Update()
    {
        // if the card is being dragged
        if (isDragging)
        {
            // set the card's position to the mouse
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            // set card's parent to the main canvas
            transform.SetParent(Canvas.transform, true);
        }
    }
}
