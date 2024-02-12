using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Round_Detector : MonoBehaviour
{
    
    public static int _rounds;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _rounds++;
        foreach (var Set_Flag in IKO_Controll.Targets)
        {
            if (Set_Flag != null && Set_Flag.GetComponent<Body_Target>() != null)
            {
                Set_Flag.GetComponent<Body_Target>().flag_move = true;
                //Debug.Log("Оборотов: " + _rounds.ToString() + " Flag: " + Set_Flag.GetComponent<Body_Target>().flag_move);
            }
        }       
        
    }
        
}
