using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragDropResources : MonoBehaviour
{
    public GameObject digitalSplicingResourceLight;
    public GameObject neurogenesisResourceLight;
    public GameObject bioAccelerationResourceLight;
    public GameObject materialAnimationResourceLight;

    public Game_Manager gm;

    private GameObject digitalSplicingColumn;
    private GameObject neurogenesisColumn;
    private GameObject bioAccelerationColumn;
    private GameObject materialAnimationColumn;

    private GameObject Canvas;
    private GameObject DropZone;

    private GameObject startParent;
    private Vector2 startPosition;
    private GameObject dropZone;
    private bool isOverDropZone;

    private bool isDragging = false;
    

    void Awake()
    {

        gm = GameObject.Find("Game_Manager").GetComponent<Game_Manager>();

        Canvas = GameObject.Find("Main Canvas");
        DropZone = GameObject.Find("P1Resource_Area");


        digitalSplicingColumn = GameObject.Find("digitalSplicingColumn");
        neurogenesisColumn = GameObject.Find("neurogenesisColumn");
        bioAccelerationColumn = GameObject.Find("bioAccelerationColumn");
        materialAnimationColumn = GameObject.Find("materialAnimationColumn");

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        dropZone = collision.gameObject;
        if (dropZone == DropZone)
        {   
            isOverDropZone = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isOverDropZone = false;
        dropZone = null;
    }

    public void StartDrag()
    {
        isDragging = true;
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
    }

    public void EndDrag()
    {
        isDragging = false;
        if ((isOverDropZone) && (dropZone == DropZone))
        {
            int index = 0;
            for (int i = 0; i < gm.handCards.Count; i++){
                if (gameObject.name == gm.handCards[i].Name)
                {
                    index = i;
                }
            } 

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

            gameObject.SetActive(false);
            gm.handCards.RemoveAt(index);

        } 
        else
        {
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
        }
    }
    
    void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            transform.SetParent(Canvas.transform, true);
        }
    }
}
