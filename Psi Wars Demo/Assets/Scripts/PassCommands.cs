using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassCommands : MonoBehaviour
{

    public GameObject Red_Resourse_Card;
    public GameObject P1Hand;

    public void OnClick()
    {
        GameObject card = Instantiate(Red_Resourse_Card, new Vector2(0, 0), Quaternion.identity);
        card.transform.SetParent(P1Hand.transform, false);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
