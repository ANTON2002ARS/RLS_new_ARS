using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body_Passive : MonoBehaviour
{
    // Происходит стробирование\\
    public static bool _is_strobing;
    
    [SerializeField]
    private ToggleAction Strobing;

    

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_is_strobing)
            return;             
        Strobing.currentState = true;          
        GameManager.Instance.AddToState(Strobing);
        _is_strobing = false;
    }
}
