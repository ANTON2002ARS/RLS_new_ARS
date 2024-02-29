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
    

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag != "Line")
            return;        
        image.SetActive(true);

        if (!IKO_Controll.Is_Help_Interference)
        {
            IKO_Controll.Is_Help_Interference = true;
            IKO_Controll.Instance.Call_Helper("На ИКО появилась помеха сначала определить её вид, " +
                "\n потом попробовать избавится от неё выбрав действие в блоках в Машине 1 => Внутри КУНГа." +
                "\n (При неправильном избавлении, помеха все равно ищезнет и будет вызвана новая)", true);
        }
           
        if(GetComponentInParent<Body_Interference>() != null && GetComponentInParent<Body_Interference>().Check_work == true)
        {
            Canvas.alpha -= 0.2f;
            if(Canvas.alpha < 0.01f)
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
