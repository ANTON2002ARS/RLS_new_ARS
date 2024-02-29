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
            IKO_Controll.Instance.Call_Helper("�� ��� ��������� ������ ������� ���������� � ���, " +
                "\n ����� ����������� ��������� �� �� ������ �������� � ������ � ������ 1 => ������ �����." +
                "\n (��� ������������ ����������, ������ ��� ����� ������� � ����� ������� �����)", true);
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
