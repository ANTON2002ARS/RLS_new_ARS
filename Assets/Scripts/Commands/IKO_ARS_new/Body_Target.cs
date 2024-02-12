using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Body_Target : MonoBehaviour
{
    [SerializeField]
    private float radius_spawn;// max 3.5f  
    // ����� ���� �� ��� \\
    public int Namber_on_IKO = 0;
    // ����(true) ��� �����(false) \\    
    [HideInInspector]
    public bool is_Our;
    // ��������(true) ��� ���������(false) \\
    [HideInInspector]
    public bool _is_group = false;
    // ������ ���� ��� ������\\   
    public int Height;
    // �������� �� ���������� ������� ���� \\
    [HideInInspector]
    public bool Check_Request;
    [HideInInspector]
    public bool Check_our;
    [HideInInspector]
    public bool Check_is_Group;
    [HideInInspector]
    public bool Check_line;
    [SerializeField]
    private GameObject Target_Our;      // ����
    [SerializeField]
    private GameObject Target_Single;   // �����                                        
    //private GameObject _target;     
    // ���������� ����� �������� �� ������ �  ����� \\
    [SerializeField]
    private int Quantity_point = 30;
    [SerializeField]
    private int Quantity_Trace = 6;
    // ��� �������� �� ���������� ������� ������� \\
    [HideInInspector]
    public bool flag_move;
    // ������� ������ � ����� �������� ���� (R = +-2.4f) \\
    [HideInInspector]
    public Vector2 startPosition;  // max 4.5f
    [HideInInspector]
    public Vector2 endPosition;    // mix 2.4f
    // ���� ����������\\
    private List<GameObject> trace_trajectories = new List<GameObject>();
    

    private void Generat_vector_circle(float radius)
    {
        // �������� ���������� ����� �� ���������� \\
        //Vector2 randomOffset = Random.insideUnitCircle * radius;
        //Vector2 start_point = new Vector2(randomOffset.x + radius, randomOffset.y + radius);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Sin(angle) * radius;
        float y = Mathf.Cos(angle) * radius;
        Vector2 start_point = new Vector2(x, y);        
        startPosition = start_point;
        // ��������� ��������������� �� ����� �� �� ���\\
        if (Random.Range(0, 2) == 1)
            endPosition = new Vector2(-1 * start_point.x, start_point.y);       
        else    
            endPosition = new Vector2(start_point.x, -1 * start_point.y); 
    }
      

    private void Turn_on_IKO(GameObject gameObject)
    {
        // ������� ���� �� ��� ��� ����������� ���� \\
        gameObject.transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, this.transform.position) + 180);
        // ������ �������� �� ����� \\
    }
   
    
    private void Create_Prefab()
    {
        // ������� ���� �� ��� \\            
        //_target = Instantiate(is_Our ? Target_Our : Target_Single, this.transform, false);        
        //_target.transform.SetParent(this.transform, false);
        // �� ���������� �� ������� ������������\\ 
        //_target.SetActive(false);        
    }   

    // ����� ���� \\
    [SerializeField]
    private int _Namber_Step = 1;
    
    
    private Vector2 Walk_line(int namber_step)
    {               
        Vector2 vector_moving = startPosition + (endPosition - startPosition) * namber_step / Quantity_point;
        return vector_moving;
    }

    
    private void Start()
    {         
        // ��������� ���� \\
        flag_move = true;
        // �������� ����� �������� \\
        Generat_vector_circle(radius_spawn);       
        Height = Random.Range(4, 11) * 10;
        // ��������� ������� \\
        this.transform.position = startPosition;        
       // Turn_on_IKO(this.gameObject);
        if (is_Our)
            this.tag = "_OUR_";
        else
            this.tag = "_SINGLE_";
       // Create_Prefab();              
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.tag == "Line" && flag_move == true)
        {            
            // �������� �� ���������� �� ��� \\
            transform.position = Walk_line(_Namber_Step);            
            // ���������� \\
            //_target.SetActive(true);
            //Turn_on_IKO(_target);            
            // �������� ���� \\
            flag_move = false;                          
            // ������� ���� �� ��� \\
            if(_Namber_Step < Quantity_Trace)
                trace_trajectories.Add(Instantiate(is_Our ? Target_Our : Target_Single, this.transform, false));            
            if(trace_trajectories.Count > 0)
                for (int i = 0; i < trace_trajectories.Count; i++)
                {
                    trace_trajectories[i].transform.position = Walk_line(_Namber_Step - i);
                    Turn_on_IKO(trace_trajectories[i]);
                    trace_trajectories[i].GetComponent<CanvasGroup>().alpha = 1f - (1f / Quantity_Trace) * i;
                }
            // ����������� ���\\
            _Namber_Step++;
            
                         
        }
    }
    private void Update()
    {        
        if (_Namber_Step >  Quantity_point)           
        {
            // �������� �� ������� ����� ����� ���������\\                
            if(!Check_our || !Check_is_Group || !Check_line)                
            {                 
                IKO_Controll iKO_Controll = new IKO_Controll();                  
                iKO_Controll.Mistakes = iKO_Controll.Mistakes + 3;
            }                              
            foreach (var trace in trace_trajectories)                    
                Destroy(trace);                
            trace_trajectories.Clear();            
            Destroy(gameObject);           
        }   
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {           
        if(collision.tag == "_SINGLE_" && this.tag == "_OUR_")
        {
            Debug.Log("���� ��������� ����� �����: " + Namber_on_IKO);
            Destroy(collision.gameObject);
        }
        else if(collision.tag == "_OUR_" && this.tag == "_OUR_")
        {
            IKO_Controll iKO_Controll = new IKO_Controll();
            iKO_Controll.Generate_Target(0, false);
            Debug.Log("�������� �����");
            Destroy(gameObject);
        }
        else if(collision.tag == "_SINGLE_" && this.tag == "_SINGLE_")
        {
            IKO_Controll iKO_Controll = new IKO_Controll();
            iKO_Controll.Generate_Target(0, true);
            Debug.Log("�������� ������");
            Destroy(gameObject);
        }
    }
}
