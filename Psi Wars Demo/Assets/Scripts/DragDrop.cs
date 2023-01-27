using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDrop : MonoBehaviour
{

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
        DropZone = GameObject.Find("P1Resource_Area");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isOverDropZone = true;
        dropZone = collision.gameObject;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isOverDropZone = false;
        dropZone = null;
    }

    public void StartDrag()
    {
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
        isDragging = true;

    }

    public void EndDrag()
    {
        isDragging = false;
        if (isOverDropZone)
        {
            transform.SetParent(dropZone.transform, false);
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
