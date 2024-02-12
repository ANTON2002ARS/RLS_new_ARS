using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Domain : MonoBehaviour
{
    [SerializeField]    
    private GameObject image;

    void Start()
    {
        image.SetActive(false);
    }
        

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Line")
        {
            image.SetActive(true);
        }
    }
}
