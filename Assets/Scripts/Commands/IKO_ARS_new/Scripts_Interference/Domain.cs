using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Domain : MonoBehaviour
{
    [SerializeField]    
    private GameObject image;

    [SerializeField]
    private CanvasGroup Canvas;

    void Start()
    {
        image.SetActive(false);
    }
        

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag != "Line")
            return;        
        image.SetActive(true);
           
        if(GetComponentInParent<Body_Interference>().Check_work == true)
        {
            Canvas.alpha -= 0.4f;
            if(Canvas.alpha < 0.2f)
            {
                GetComponentInParent<Body_Interference>().Delete();
            }
        }

        if(this.tag == "PASSIVE" && Body_Passive._is_strobing == true)            
        {
            Canvas.alpha -= 0.4f;           
        }                 
    }
}
