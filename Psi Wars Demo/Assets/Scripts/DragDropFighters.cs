using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDropFighters : MonoBehaviour
{

    public Game_Manager gm;

    private GameObject Canvas;
    private GameObject DropZone;
    private GameObject startParent;
    private Vector2 startPosition;
    private GameObject dropZone;
    private bool isOverDropZone;

    private bool isDragging = false;
    

    void Start()
    {
        Canvas = GameObject.Find("Main Canvas");
        DropZone = GameObject.Find("P1Fighters_Area");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        dropZone = collision.gameObject;
        if (dropZone == DropZone){   
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
            transform.SetParent(dropZone.transform, false);

            int index = 0;
            for (int i = 0; i < Game_Manager.handCards.Count; i++){
                if (gameObject.name == Game_Manager.handCards[i].Name){
                    index = i;
                }
            }

            Game_Manager.greenResourcesAvailable -= Game_Manager.handCards[index].GreenCost;
            Game_Manager.blueResourcesAvailable -= Game_Manager.handCards[index].BlueCost;
            Game_Manager.redResourcesAvailable -= Game_Manager.handCards[index].RedCost;
            Game_Manager.purpleResourcesAvailable -= Game_Manager.handCards[index].PurpleCost;
            
            Game_Manager.handCards.RemoveAt(index);

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
