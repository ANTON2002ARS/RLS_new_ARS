using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IKO_Controll : MonoBehaviour
{
    [Header("Display Objects")]
    [SerializeField]
    private CanvasGroup Grid;
    [SerializeField]
    private GameObject LineObject;
    [SerializeField]
    private GameObject EdgeObject;
    [SerializeField]
    public Transform TargetsFolder;
    [SerializeField]
    private CanvasGroup _ikoPanel;
    [SerializeField]
    private Transform InterferenceFolder;

    [Header("Controls objects")]
    [SerializeField]
    private Scrollbar BrightnessController;
    [SerializeField]
    private Scrollbar Scrobing_Line;
    [SerializeField]
    private Button StartButton;    
    [SerializeField]
    private Button Restart_Button;
    [SerializeField]
    private Button Rpm6_Btn;
    [SerializeField]
    private Button Rpm12_Btn;

    [Header("Settings")]
    [SerializeField]
    private float LineRotationSpeed_6rpm = -36f;
    [SerializeField]
    private float LineRotationSpeed_12rpm = -72f;   

    #region Work mode

    [System.Serializable]
    public enum IkoWorkMode
    {
        Rpm6,
        Rpm12,
    }

    private IkoWorkMode _mode;
    public IkoWorkMode WorkMode
    {
        get => _mode;
        set
        {
            _mode = value;
            switch (value)
            {
                case IkoWorkMode.Rpm12:
                    Rpm12_Btn.interactable = false;
                    Rpm6_Btn.interactable = true;
                    break;
                case IkoWorkMode.Rpm6:
                default:
                    Rpm12_Btn.interactable = true;
                    Rpm6_Btn.interactable = false;
                    break;
            }
        }
    }

    private float LineRotationSpeed
    {
        get
        {
            switch (WorkMode)
            {
                case IkoWorkMode.Rpm12:
                    return LineRotationSpeed_12rpm;
                case IkoWorkMode.Rpm6:
                default:
                    return LineRotationSpeed_6rpm;
            }
        }
    }

    #endregion
        
    [Header("Target and PRS Prefabs")]
    [SerializeField]
    private GameObject Target_Flying;
    [SerializeField]
    private GameObject PRS_target;
   
    [Header("Interference Prefabs")]
    [SerializeField]
    private List<GameObject> Passive_Prefab;
    [SerializeField]
    private List<GameObject> From_local_Prefab;
    [SerializeField]
    private List<GameObject> NIP_Prefab;
    [SerializeField]
    private List<GameObject> Active_noise_Prefab;
    [SerializeField]
    private List<GameObject> Response_Prefab;
    [SerializeField]
    private float _passiveIntRadius;

    [Header("Test buttons")]
    [SerializeField]
    public Dropdown Choice_target;
    [SerializeField]
    private List<Text> Text_targets_fix;
    [SerializeField]    
    private List<Button> Target_Buttons;
    [SerializeField]
    private Text Data_Height;
    [SerializeField]
    private List<Button> Interference_Buttons;
    
    public Text Report;
    [SerializeField]
    private InputField Status_Target;
    [SerializeField]
    private InputField Status_PRS;
    [SerializeField]
    private Button Stop_Time;
    [SerializeField]
    private Button Check_Line;

    [Header("Help Mode")]
    [SerializeField]
    private Button Children_Button_Set;
    [SerializeField]
    private Helper_Testing Children_mode_Helper;
    private bool _has_help;
    /*public static bool Is_Help_Target;
    public static bool Is_Help_Interference;*/

    [Header("______")]
    private const float _defaultBrightness = 0.5f;        
    private bool _hasStarted;
    public string Str_Mistakes = null;
    // ������ ���� ����������� ����� �� ��� \\
    public static List<GameObject> Targets = new List<GameObject>();
    // ������ ��� �� ����� \\
    public static List<GameObject> PRS_of_target = new List<GameObject>();
    // ������ ����� \\
    public static Stack<Body_Interference> Interferenses_ = new Stack<Body_Interference>();
    [Header("Mistakes Check")]
    // ���� ������ \\
    [SerializeField]
    public int Max_Mistakes = 10;
    private int _currentMistakes = 0;
        
    public int Mistakes
    {
        get => _currentMistakes;
        set
        {
            _currentMistakes = value;
            if (_currentMistakes >= Max_Mistakes)
            {
                _hasStarted = false;
                GameManager.Instance.FailCheck();
            }
        }
    }
    
       
    [Header("Strob control")]
    [SerializeField]
    private GameObject _strobContainer;    
    [SerializeField]
    [Range(0, 1)]
    private float _strobTolerance;
    [SerializeField]
    private float _interferenceFadeDist;
    [SerializeField]
    private float _minInterferenceBrightness;    
    
    public static Vector3 IkoCenter => Instance?.LineObject.transform.position ?? Vector3.zero;
        
    public static IKO_Controll Instance { get; private set; }    

    public event UnityAction OnReset;

    // ��������� ����� \\
    public void Stop_Test(bool _is_stop) => _hasStarted = !_is_stop;

    // ���������� ����� ��� ����� \\
    [SerializeField]
    private int Quentity_Targets_need = 8;
    // ���������� ������������ ����� \\
    private int is_quentity;
    private int _quentity_kill;
    // �������� � �������� \\
    private float interval = 30f;
    private float timer = 0f;
    // �������� ����\\
    private int choice_target;

    private void Awake() => Instance = this;
  
    void Start()
    {
        // ������������� �������� � ����� ��������\\
        WorkMode = IkoWorkMode.Rpm6;        
        BrightnessController.onValueChanged.AddListener(BrightnessChanged);
        BrightnessController.value = _defaultBrightness;
        gameObject.SetActive(true);
        Rpm6_Btn.onClick.AddListener(() => WorkMode = IkoWorkMode.Rpm6);
        Rpm12_Btn.onClick.AddListener(() => WorkMode = IkoWorkMode.Rpm12);
    }

        
    void Update()
    {
        if (!_hasStarted) return;
        // �������� ����� \\
        var angles = LineObject.transform.localEulerAngles;
        var lastAngle = angles.z;
        angles.z += LineRotationSpeed * Time.deltaTime;
        LineObject.transform.localEulerAngles = angles;

        // �������� �� ���� ����� �� ��� \\                
        for (int i = 0; i < Targets.Count; i++)
        {
            // ������� ������ � ���� ���� ��� ���������� \\
            if (Targets[i] == null || Targets[i].GetComponent<Body_Target>().End_Player == true)
            {
                Text_targets_fix[i].gameObject.SetActive(false);
                Report.text = Str_Mistakes = "�������� ����";
                Report.color = Color.black;
                _quentity_kill++;
                // ������� ����� ���� � ����������� ���\\
                Remove_Option(i);
                Targets.RemoveAt(i);                
            }
            else
            {
                // �������� ���� �� ���\\
                if(i == Choice_target.value)
                {
                    choice_target = i;
                    Text_targets_fix[i].color = Color.red;
                    Data_Height.text = "������ � �����������: \n" + Targets[i].GetComponent<Body_Target>().Height + " �����������";
                }
                else
                    Text_targets_fix[i].color = Color.black;

                // ������������� ������ ��� ������\\   
                Text_targets_fix[i].transform.position = Targets[i].transform.position;                    
                Text_targets_fix[i].text = "0" + Targets[i].GetComponent<Body_Target>().Namber_on_IKO.ToString();                   
                Text_targets_fix[i].gameObject.SetActive(true);
            }   
            
        }        

        // ����������� ������ �� �������� ������� \\
        timer += Time.deltaTime;       
        if (timer >= interval)
        {
            if(is_quentity < Quentity_Targets_need)
            {
                // ������� ���� \\            
                Generate_Target();
                // ������� ������\\
                Generate_Interference();
                // ������� ��� \\
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = 4.0f;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius;
                Vector2 start_prs = new Vector2(x, y);
                // �������� ��� �� ��� \\
                if(Random.Range(0, 9) == 1)
                    Generate_PRS(start_prs, 0);
               
                Report.text = Str_Mistakes = "��������� ����� ����," +
                              "\n    ���-�� ����������� �����:  " + is_quentity +
                              "\n    ���-�� �������� �����:      " + _quentity_kill +
                              "\n    ���-�� ������:              " + Mistakes;
                Report.color = Color.black;
                Changed_Text_Button();                
            }            
            // ���������� ������ \\           
            timer = 0f;            
        }

        // �������� ���� ���� ��� ���� ���������� \\
        if(Targets.Count == 0 && _quentity_kill == Quentity_Targets_need)
        {
            _hasStarted = false;
            GameManager.Instance.PassCheck();
        }
    }
        

    public void Changed_Text_Button()
    {
        // ����� �� ������ ������\\
        Text text_botton = Target_Buttons[0].GetComponentInChildren<Text>();
        text_botton.text = "������ ����";
        //Debug.Log("����� ������");
    }
    

    public void Remove_Option(int optionIndex)
    {
        // �������� ������ ���� ����� \\
        List<Dropdown.OptionData> options = Choice_target.options;
        // ������� ����� � �������� �������� \\
        options.RemoveAt(optionIndex);
        // ��������� ������ ����� \\
        Choice_target.options = options;        
        Choice_target.value = 0;
    }
    

    public int Find_Free_Number(List<int> numbers)
    {
        // ���� ��������� ����� ��� ����\\
        numbers.Sort(); // ��������� ������ �����
        int lastNumber = -1; // ���������� ����� �� ������
        // ������� ����������� �����
        foreach (int number in numbers)
        {
            if (number - lastNumber > 1)
                return lastNumber + 1;
            lastNumber = number;
        }
        // ���� ��� ����������� �����, ���������� ��������� ����� �� ���������
        return lastNumber + 1;
    }

    // �������� ������������ �����\\
    public void BrightnessChanged(float value) => Grid.alpha = value;  
    
    // ��� �������� ��� ���� ��� �������\\
    private bool _close_Iko;
    public void OpenIko()
    {        
        _ikoPanel.alpha = 1.0f;
        _ikoPanel.interactable = true;
        _ikoPanel.blocksRaycasts = true;    
        // ���� ���� ������������������ ��� ���������� �� ������ �� ��������� �� ����������\\ 
        if (InterferenceFolder.transform.childCount > 0)
            Check_Interference_Bloks();
        // ������� ������ �������� ��� ������\\
        GameManager.Instance.Reset_Blocks_Action();
        // ��������� ��� ��� �� �� ����� ��� ���������� \\
        if (_close_Iko && !_stop_antenna)
        {
            Debug.Log("START time");
            _hasStarted = true;
        }
        else if (_close_Iko && !_hasStarted)
        {
            if (_stop_antenna)
                _hasStarted = false;           
            else
                _hasStarted = true;
        }                             
    }

    // �� ������ ����\\
    public void CloseIko()
    {              
        _ikoPanel.alpha = 0.0f;
        _ikoPanel.interactable = false;
        _ikoPanel.blocksRaycasts = false;
    }
    public void Close_IKO()
    {
        Call_Helper("��� ����������� �� ���, \n ��� ������� ������ ����� ��������� ������������ ���������� �� ��.", false);

        if (_hasStarted)
            Stop_Test(true);
       
        Debug.Log("STOP time"); 
        _close_Iko = true;

        GameManager.Instance.Reset_Blocks_Action();
        GameManager.Instance.Clear_Action();
    }

    // ������������� ������� \\    
    public void Start_Strobing()
    {        
        // �������� ����� \\
        float angles_strib = Scrobing_Line.value;
        var angles = LineObject.transform.localEulerAngles;
        var lastAngle = angles.z;
        angles.z += LineRotationSpeed *(float)(angles_strib / 10);
        LineObject.transform.localEulerAngles = angles;

        if (InterferenceFolder.transform.childCount != 1)
            return; ;

        if (Interferenses_ == null)
            return;

        var interference = Interferenses_.Peek().gameObject;
        
        if(interference.tag == "PASSIVE")
                Body_Passive._is_strobing = true;        
    }
    
    //��������� �� ���������� �� ������\\
    public void Check_Interference_Bloks()
    {
        // ��������� ��� ���� ��������\\
        if (Interferenses_.Count < 1)
            return;
        // �� ����� ���� \\
        if (Interferenses_.Peek() == null)
            return;

        Debug.Log("TAG: " + Interferenses_.Peek().gameObject.tag);
        // �� Tag ���������� �� �������� �� ���������� �� ������\\
        if (!GameManager.Instance.Check_Interference(Interferenses_.Peek().gameObject.tag))
        {
            Mistakes++;
            Report.text = "������, �� ���������� ���������� �� �����, ���-�� ������: " + Mistakes;
            Report.color = Color.red;
        }
        else
        {
            Report.text = "���������, ���������� �� �����, ���-�� ������: " + Mistakes;
            Report.color = Color.green;
        }
        // ���� �� ���������� �� ������\\
        Interferenses_.Pop().Check_work = true;       
        //GameManager.Instance.Clear_Action();
    }

    // �������� � ��������� �������\\
    public void EnableStrobControl() => _strobContainer.SetActive(true);
    public void DisableStrobControl() => _strobContainer.SetActive(false);
    // �������� ����� ����\\    
    public void Generate_Target()
    {
        // ��� ���� ������ �� ������\\
        if (Target_Flying == null)
            return;

        GameObject target = Instantiate(Target_Flying, TargetsFolder);
        target.transform.SetParent(TargetsFolder.transform);        
        // ��������� ����� ���� \\
        is_quentity++;
        // ��������� ���� � ������ ������ \\
        Targets.Insert(0, target);
        // �������� �� ��������� 00 \\
        if (Targets.Count > 1)
        {
            if(Targets[1].GetComponent<Body_Target>().Namber_on_IKO == 0)
            {
                Report.text = Str_Mistakes = "���� �� �������, ������ �� �������� ����";
                Report.color = Color.red;
                GameManager.Instance.FailCheck();
            }
        }
        // �������� ������������ ��������� �����
        List<Dropdown.OptionData> options = Choice_target.options;
        Dropdown.OptionData _target = new Dropdown.OptionData();
        _target.text = "���� 00";
        options.Insert(0, _target);
        // ������������� ����������� ��������� ����� � Dropdown
        Choice_target.options = options;

    }
    // ����������� ����\\
    public void Request_Targets()
    {
        if (!_hasStarted) return;
        // �� ������ ���� ������������� ������� ������\\
        int namber = 0;
        if (Targets[namber] != null )
        {
            if(Targets[namber].GetComponent<Body_Target>().Check_Request == false)
            {
                Targets[namber].GetComponent<Body_Target>().Check_Request = true;
                Text text_botton = Target_Buttons[0].GetComponentInChildren<Text>();
                if(_quentity_kill > 0)
                {
                    List<int> Namber_on_IKO = new List<int>();
                    foreach (var target in Targets)
                        Namber_on_IKO.Add(target.GetComponent<Body_Target>().Namber_on_IKO);                 
                    int _namber_on_IKO =  Find_Free_Number(Namber_on_IKO);
                    // ����� ������� ���� ����������� ����� � ����������� 00 \\ 
                    Targets[namber].GetComponent<Body_Target>().Namber_on_IKO = _namber_on_IKO;                    
                    // �������� ���� � ����� ����� \\                
                    Choice_target.options[namber].text = "���� 0" + _namber_on_IKO;                    
                }
                else
                {
                    // ����� ������� ���� ����������� ����� � ����������� 00 \\   
                    Targets[namber].GetComponent<Body_Target>().Namber_on_IKO = Targets.Count;
                    // �������� ���� � ����� ����� \\
                    Choice_target.options[namber].text = "���� 0" + Targets.Count;                  
                }
                // ������������� ����� �������� ��� ��������� ������� \\
                Choice_target.SetValueWithoutNotify(namber);                
                Choice_target.GetComponentInChildren<Text>().text = Choice_target.options[namber].text;
                //Choice_target.options.Add(new Dropdown.OptionData() { text = "���� 0" + Targets.Count });
                if (Targets[namber].GetComponent<Body_Target>().is_Our == true)
                    text_botton.text = "���� 0" + Targets[namber].GetComponent<Body_Target>().Namber_on_IKO.ToString() + ", ���� �����";
                else
                    text_botton.text = "���� 0" + Targets[namber].GetComponent<Body_Target>().Namber_on_IKO.ToString() + ", ��� ������";
            }
            else
            {
                Report.text = Str_Mistakes = "������ ������ � ����������� ����, ���-�� ������: " + Mistakes;
                Report.color = Color.red;
                Mistakes++;                
            }
        }
    }
    // ��� ����� ���� ��������� ��� ���������\\
    public void Button_Test_Group(bool _is_group)
    { 
        if (!_hasStarted) return;
        // ����� �� ��� ������� ���� \\
        int namber = Choice_target.value;
        if (Targets[namber].GetComponent<Body_Target>()._is_group != _is_group)
        {
            Mistakes++;
            Report.text = Str_Mistakes = "������ �� ������� ���-�� �����, ���-�� ������: " + Mistakes;
            Report.color = Color.red;
        }
        else
        {
            Report.text = Str_Mistakes = "��������� ���-�� ������: " + Mistakes;
            Report.color = Color.green;
            // ���� ��������� \\
            Targets[choice_target].GetComponent<Body_Target>().Check_is_Group = true;
        }           
    }
      
    // ��� ����� ��� ���� ��� ����� \\
    public void Button_Test_Our( bool _is_Our)
    {       
        if (!_hasStarted) return;
        int namber = Choice_target.value;
        if(Targets[namber].GetComponent<Body_Target>().Check_Request == true)
        {
            if (Targets[namber].GetComponent<Body_Target>().is_Our != _is_Our)
            {
                Report.text = Str_Mistakes = "������ �� ������� ����, ���-�� ������: " + Mistakes;
                Report.color = Color.red;
                Mistakes++;
            }
            else
            {
                Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                Report.color = Color.green;
                // ���� ��������� \\
                Targets[choice_target].GetComponent<Body_Target>().Check_our = true;
            }
                        
        }       
    }

    // ������ ���� ��������� \\
    public void Restart()
    {
        _hasStarted = false;
        // ������� ��� ������ � ����\\
        foreach (var target in Targets)
            if (target != null)
                Destroy(target);
        Targets.Clear();

        foreach (var prs in PRS_of_target)
            if (prs != null)
                Destroy(prs);                
        PRS_of_target.Clear();

        is_quentity = _quentity_kill = 0;
        timer = 0f;
        // ������� ������� �����\\
        foreach (var text in Text_targets_fix)
        {
            text.gameObject.SetActive(false);
            text.transform.position = Vector2.zero;
        }
        Choice_target.options.Clear();
        Choice_target.GetComponentInChildren<Text>().text = null;
        // ������ �������\\        
        while (InterferenceFolder.childCount > 0)
            DestroyImmediate(InterferenceFolder.GetChild(0).gameObject);  
        // ����� � ������\\            
        LineObject.transform.localEulerAngles = new Vector3(0, 0, 90);
        StartButton.interactable = true;
        OnReset?.Invoke();
        WorkMode = IkoWorkMode.Rpm6;
        Mistakes = 0;
        Report.text = Str_Mistakes = "";
        Report.color = Color.black;
        // ������� ���������\\
        _has_help = false;        
        Children_Button_Set.gameObject.SetActive(true);
    }
    // ������ ����� \\
    public void Start_Test()
    {
        // start line \\
        if (_hasStarted) return;
        _hasStarted = true;
        // � ������\\
        if (Status_Target.text == "������ 66")
            Max_Mistakes = 66;
        StartButton.interactable = false;        
        Report.text = Str_Mistakes = "  ������ ������ ������  ";
        Report.color = Color.black;
        Children_Button_Set.gameObject.SetActive(false);
        Call_Helper("����� ����� ����� �������� ����� � ����������� �����", true);
        // ������� ���� \\
        Generate_Target();
        // ������� ������\\
        Generate_Interference();
    }    
    // ������������� ���� \\
    private bool _stop_antenna;    
    public void Stop_timer()
    {        
        if (!_stop_antenna)
        {
            _stop_antenna = true;
            Report.text = "�����";
            Report.color = Color.black;           
            Stop_Test(true);
            Stop_Time.GetComponentInChildren<Text>().text = "�����";
            Stop_Time.GetComponent<Image>().color = Color.red;
            Restart_Button.interactable = false;
            StartButton.interactable = false;
        }
        else
        {
            _stop_antenna = false;
            Report.text = "�����������";
            Report.color = Color.black;
            if (_hasStarted == false && Targets.Count >= 1)
                Stop_Test(false);                
            Stop_Time.GetComponentInChildren<Text>().text = "���������� �������\n(�����)";
            Stop_Time.GetComponent<Image>().color = new Color(127 / 255f, 127 / 255f, 127 / 255f);
            Restart_Button.interactable = true;
            StartButton.interactable = true;
        }
        Scrobing_Line.value = 0;
    }
    
        
    private int Find_Azimuth(Vector2 of_Target)
    {
        // ��������� ���� ����� ��������� \\
        float angle = Vector3.SignedAngle(Vector2.up, of_Target, Vector3.forward);   
        // ������������ ����, ����� �� ��� ������������� \\
        angle = -1 * angle;
        if (angle < 0)
            angle += 360f;        
        //Debug.Log("����: " + angle + " �������� �� ������� �������");
        return (int)angle;
    }
       
    // ��� �������� ���� � ������� �������� \\
    private bool is_Within_Range(int azimuth_user, Vector2 of_target)
    {
        int azimuth_target = Find_Azimuth(of_target);
        float lowerLimit = azimuth_target * 0.9f; // 90% �� ������� �����
        float upperLimit = azimuth_target * 1.1f; // 110% �� ������� �����
        return azimuth_user >= lowerLimit && azimuth_user <= upperLimit;
    }
    // ���� ���������� �� ���� \\
    private bool Find_Limit(int long_target, float ring)
    {        
        float lowerLimit = (float)((ring - 1) * 10);        
        float upperLimit = (float)((ring + 1) * 10);        
        return long_target * 10 >= (int)lowerLimit && long_target * 10 <= (int)upperLimit;
    }
    // ��������� ������ � ���� \\
    public void Check_line_Targets()
    {
        if (!_hasStarted) return;
        Call_Helper("��� ������� � ���� �������� ���������� � ���������� � �����������: \n" +
            " �00(������ ����� ����) - 00(����� ����) - 000 (������) - 000 (���������)�", true);
        string input = Status_Target.text;        
        string[] numbers = input.Split('-');
        if (numbers.Length != 4)
        {
            Report.text = "�� �� ��� �����, ������ 00-00-000-000";
            Report.color = Color.blue;
            return;
        }
        int free_namber = 0;
        int number_target = 0;
        int azimuth = 0; 
        int ring_to_long = 0;     
        try
        {
            free_namber = int.Parse(numbers[0]);
            number_target = int.Parse(numbers[1]);
            azimuth = int.Parse(numbers[2]);
            ring_to_long = int.Parse(numbers[3]);
        }
        catch
        {
            Report.text = "������� �����, �� �������";
            Report.color = Color.blue;
        }
        // ������� ���������� ���� \\
        Vector2 of_target = Targets[choice_target].GetComponent<Transform>().position;        
        if(number_target != Targets[choice_target].GetComponent<Body_Target>().Namber_on_IKO)
        {
            Report.text = "�� ��� ��� ���� ��������";
            Report.color = Color.red;
            return;
        }        
        if (!is_Within_Range(azimuth, of_target))
        {
            Report.text = "������ �������, ���������: " + azimuth;
            Report.color = Color.red;
            Mistakes++;
            return;
        }                
        float ring_target = (float)(of_target.magnitude * 15 / 4);
        //ring_to_long != Mathf.Round(ring_target) *10
        if (Find_Limit(ring_to_long, ring_target))
        {
            Report.text = "������ ���������, ���������: " + Mathf.Round(ring_target * 10);
            Report.color = Color.red;
            Mistakes++;
            return;
        }
        // ���� ��������� \\
        Targets[choice_target].GetComponent<Body_Target>().Check_line = true;
        Status_Target.text = "";
        Report.text = "���������, ���-�� ������: " + Mistakes;
        Report.color = Color.green;
        return;
    }

    private int exclude = 1;
    // ������� ������ �� ��� \\
    public void Generate_Interference()
    {
        GameObject Interferense;
        //Random.Range(0, 4)
        string _interferense_tag;
        List<int> numbers = new() { 0, 1, 2, 3, 4 }; // ������� ������ ����� �� 0 �� 4, �� ����������� �������� 2
        numbers.Remove(exclude); // ������� �����-���������� �� ������
        int randomIndex = Random.Range(0, numbers.Count); // ���������� ��������� ������ �� ��������� �����
        //numbers[randomIndex]
        switch (numbers[randomIndex])
        {
            case 0:
                Interferense = Instantiate(Passive_Prefab[Random.Range(0, Passive_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "PASSIVE";
                exclude = 0;
                break;
            case 1:
                Interferense = Instantiate(From_local_Prefab[Random.Range(0, From_local_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "FROM_LOCAL";
                exclude = 1;
                break;
            case 2:
                Interferense = Instantiate(NIP_Prefab[Random.Range(0, NIP_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "NIP";
                exclude = 2;
                break;
            case 3:
                Interferense = Instantiate(Active_noise_Prefab[Random.Range(0, Active_noise_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "ACTIVE_NOISE";
                exclude = 3;
                break;
            default:
                Interferense = Instantiate(Response_Prefab[Random.Range(0, Response_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "RESPONSE";
                exclude = 4;
                break;
        }
        Call_Helper("�� ��� ��������� ������ ������� ���������� � ���, " +
                "\n ����� ����������� ��������� �� �� ������ �������� � ������ � ������ 1 => ������ �����." +
                "\n (��� ������������ ����������, ������ ��� ����� �������� � ����� ������� �����)", true);
        if (Interferenses_.Count > 0 && Interferenses_.Peek() != null)
        {
            var OLD_interferense = Interferenses_.Peek();            
            if(OLD_interferense.GetComponent<Body_Interference>().Check_Test == false)
            {
                Mistakes++;
                Report.text = Str_Mistakes = "������, �� ��������� ������ ������";
                Report.color = Color.red;
            }            
        }
        Interferense.GetComponent<Body_Interference>().tag = _interferense_tag;
        Interferenses_.Push(Interferense.GetComponent<Body_Interference>());
    }
    // ��������� ��� ������ �� ��� �� �������\\
    public void Check_Interference(int namber_button)
    {
        if (!_hasStarted) return;
        // ��� ��� ���� �� ���\\
        if (InterferenceFolder.transform.childCount == 0 || Interferenses_.Count == 0)
            return;           
        // �������� ��������� ������ �� ���\\
        Body_Interference interference = Interferenses_.Peek();
        if (interference == null)
            return;
        interference.Check_Test = true;
        // ��������� ��� ��������� ��������� ������ \\
        switch (namber_button)
        {
            case 0:
                if(interference.tag == "PASSIVE")
                {
                    Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("�� ��� ��������� ������ ��� ���������� ����� ���������� ������������� �� ������: \n " +
                        "���-71 => �������� ����� ������ �� ������������� \n " +
                        "���-72 => � ��������� ������", true);
                    return;
                }
                break;
            case 1:
                if (interference.tag == "FROM_LOCAL")
                {
                    Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("�� ��� ������� �������� ��� ���������� ����� ���������� ������������� �� ������: \n" +
                        "\n �-71 => ������� ��������" +
                        "\n �-71 => ������� ��������" +
                        "\n O-71 => ������ ���������" +
                        "\n O-71 => ������� ����������" +
                        "\n O-71 => ������" +
                        "\n O-71 => ������" +
                        "\n ���-71 => ������� ������� ����������", true);
                    return;
                }
                break;
            case 2:
                if (interference.tag == "NIP")
                {
                    Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("�� ��� ��� ��� ���������� ����� ���������� ������������� �� ������: \n" +
                        "\n ��-71 => �� ����. � ��������� ��", true);
                    return;
                }
                break;
            case 3:
                if (interference.tag == "ACTIVE_NOISE")
                {
                    Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("�� ��� �������� ������� ��� ���������� ����� ���������� ������������� �� ������: \n" +
                        "\n ���-71 => ������������ ����" +
                        "\n ���-72=> ������", true);
                    return;
                }
                break;
            case 4:
                if (interference.tag == "RESPONSE")
                {
                    Report.text = Str_Mistakes = "���������, ���-�� ������: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("�� ��� �������� ������ ��� ���������� ����� ���������� ������������� �� ������: \n" +
                        "\n ��-71 => �� ����. � ��������� ��", true);
                    return;
                }
                break;            
        }
        Mistakes++;
        Report.text = Str_Mistakes = "������ �� ��������� ��������� ������, ���-�� ������: " + Mistakes;
        Report.color = Color.red;        
    }
    // ������� ��� \\
    public void Generate_PRS(Vector2 start_PRS , int of_target)
    {       
        // ������ ���� �������� ���� �� ���\\
        if (PRS_of_target.Count >= 2)
            return;
        if (of_target == 0)
            Call_Helper("�� ��� ����������������������� ������. ��������: ���� ��� 000-000 (������-���������)" +
                " \n � ���������� �� ���� �� ���72 => ������� ������ � ��������� ����� �������� ", true);
        else
            Call_Helper("�� ��� ����������������������� ������. ��������: ���� 00, ���� ��� 000-000 (������-���������)" +
                "\n � ���������� �� ���� �� ���72 => ������� ������ � �������� ����� �������� ", true);
        // ������� ���\\
        GameObject PRS = Instantiate(PRS_target, TargetsFolder, false);
        PRS.transform.SetParent(TargetsFolder.transform);
        PRS.GetComponent<Body_PRS>().Start_PRS = start_PRS;
        PRS.GetComponent<Body_PRS>().Of_Target = of_target;
        // ��������� � ������  \\
        PRS_of_target.Add(PRS);      
    }
    // ��������� ��� ��������� ������� � ���\\
    public void Check_Line_PRS()
    {
        // �������� ������ �� ������\\
        //���� 00, ���� ��� 000-000
        string input = Status_PRS.text;
        List<int> data_report = new List<int>();
        string[] words = input.Split(' ', ',', '-');        
        foreach (string word in words)
        {
            int num;
            if (int.TryParse(word, out num))
                data_report.Add(num);         
        }
        // �������� �������� ������\\
        int number_target = 0;
        int azimuth;
        int ring_to_long;
        // ���� ��� �� ���� ��� ��������� \\
        if (data_report.Count == 2)
        {  
            azimuth = data_report[0];
            ring_to_long = data_report[1];
        }
        else if(data_report.Count == 3)
        {
            number_target = data_report[0];
            azimuth = data_report[1];
            ring_to_long = data_report[2];
        }
        else
        {
            Report.text = "�� �� ��� �����, ������: ���� 00, ���� ��� 000-000 ���" +
                                                "\n ���� ��� 000-000";
            Report.color = Color.blue;
            return;
        }

        Debug.Log("number_target: " + number_target + " azimuth: " + azimuth + " ring_to_long: " + ring_to_long);
        Debug.Log("PRS_of_target.Count: " + PRS_of_target.Count);                
        // ��������� ������ � ���� \\
        foreach (var prs in  PRS_of_target)
        {            
            if(prs != null && prs.GetComponent<Body_PRS>().Of_Target == number_target)
            {
                Vector2 position_prs = prs.GetComponent<Transform>().position;

                Debug.Log("position_prs: " + position_prs);

                if (!is_Within_Range(azimuth, position_prs))
                {
                    Report.text = "������ �������, ���������: " + azimuth;
                    Report.color = Color.red;
                    Mistakes++;
                    return;
                }

                float ring_target = (float)(position_prs.magnitude * 15 / 4);
                //ring_to_long != Mathf.Round(ring_target) *10
                if (Find_Limit(ring_to_long, ring_target))
                {
                    Report.text = "������ ���������, ���������: " + Mathf.Round(ring_target * 10);
                    Report.color = Color.red;
                    Mistakes++;
                    return;
                }

                prs.GetComponent<Body_PRS>().Check_Status = true;
                Status_PRS.text = "���� , ���� ��� ";

                Report.text = "���������, ���-�� ������: " + Mistakes;
                Report.color = Color.green;

                return;
            } 
        }
        Report.text = "�� ��� ��� ���� ��������";
        Report.color = Color.red;
    }
    // �������� ������ �������� � ��� ��� ����������\\ 
    public void Check_Flickering(GameObject PRS)
    {
        if (!PRS.GetComponent<Body_PRS>().Mode_Frickering)
            Kill_RLS();
        // ������� ��� == ��������� \\
        PRS_of_target.Remove(PRS);
        Destroy(PRS);        
        Report.text = Str_Mistakes = "����������������������� ������ => ����a����";
        Report.color = Color.green;
    }
    // ����������� ��� ����� \\
    private void Kill_RLS()
    {
        // ���� �������� ��� ����������\\
        if (Random.Range(0, 1) == 1)
            return;

        _hasStarted = false;
        Report.text = Str_Mistakes = "��� ���� ���������� ����������������������� ��������";
        Report.color = Color.red;        
        GameManager.Instance.FailCheck();
    }

    // ���������� ��� ������� ����� �������� � ��� \\
    public void Set_Mode_Frickering()
    {
        foreach (var prs in PRS_of_target)
            if(prs != null)
                prs.GetComponent<Body_PRS>().Mode_Frickering = true;
    }

    // ��������� �������� ������\\
    public void Test_children_mode()
    {
        _has_help = true;
        Children_Button_Set.gameObject.SetActive(false);
        Max_Mistakes = 1000;
    }
        
    // ������� ������� �����\\
    public void Call_Helper(string text, bool _can_continue)
    {
        if (!_has_help)
            return;            
        Children_mode_Helper.Call_Helper(text, _can_continue);

    }

}

[System.Serializable]
public enum Interference_Type
{
    None = 0,
    Passive = 1,
    LocalObjects = 2,
    NonlinearImpulse = 3,
    ActiveNoice = 4,
    ResponseImpulse = 5,
    ActiveImitating = 6,
}