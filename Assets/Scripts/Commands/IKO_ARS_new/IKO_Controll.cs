using System.Collections;
using System.Collections.Generic;
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
        
    [Header("Target Prefabs")]
    [SerializeField]
    private GameObject Target_Flying;

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
    private Button Stop_Time;
    [SerializeField]
    private Button Check_Line;

    [Header("Help Mode")]
    [SerializeField]
    private Button Children_Button_Set;
    [SerializeField]
    private Helper_Testing Children_mode_Helper;
    private bool _has_help;
    public static bool Is_Help_Target;
    public static bool Is_Help_Interference;

    [Header("______")]
    private const float _defaultBrightness = 0.5f;        
    private bool _hasStarted;
    public string Str_Mistakes = null;
    // Список всех появившихся целей на ико \\
    public static List<GameObject> Targets = new List<GameObject>();
    // Список помех \\
    public static Stack<Body_Interference> Interferenses_ = new Stack<Body_Interference>();
    [Header("Mistakes Check")]
    // сбор ошибок \\
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

    // остановка теста \\
    public void Stop_Test(bool _is_stop) => _hasStarted = !_is_stop;

    // Количество целий для теста \\
    [SerializeField]
    private int Quentity_Targets_need = 8;
    // количество обработанных целей \\
    private int is_quentity;
    private int _quentity_kill;
    // интервал в секундах \\
    private float interval = 20f;
    private float timer = 0f;
    // Выбраная цель\\
    private int choice_target;    

    private void Awake()
    {
        Instance = this;
    }


    void Start()
    {
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
        // Вращение линии \\
        var angles = LineObject.transform.localEulerAngles;
        var lastAngle = angles.z;
        angles.z += LineRotationSpeed * Time.deltaTime;
        LineObject.transform.localEulerAngles = angles;
                
        for (int i = 0; i < Targets.Count; i++)
        {
            if (Targets[i] == null || Targets[i].GetComponent<Body_Target>().End_Player == true)
            {
                Text_targets_fix[i].gameObject.SetActive(false);
                Report.text = Str_Mistakes = "Удаление цели";
                Report.color = Color.black;
                _quentity_kill++;
                Remove_Option(i);
                Targets.RemoveAt(i);                
            }
            else
            {
                if(i == Choice_target.value)
                {
                    choice_target = i;
                    Text_targets_fix[i].color = Color.red;
                    Data_Height.text = "Данные с высотометра: \n" + Targets[i].GetComponent<Body_Target>().Height + " Гектометров";
                }
                else
                    Text_targets_fix[i].color = Color.black;
                    
                Text_targets_fix[i].transform.position = Targets[i].transform.position;                    
                Text_targets_fix[i].text = "0" + Targets[i].GetComponent<Body_Target>().Namber_on_IKO.ToString();                   
                Text_targets_fix[i].gameObject.SetActive(true);
            }   
            
        }        

        // увеличиваем таймер на значение времени \\
        timer += Time.deltaTime;       
        if (timer >= interval)
        {
            if(is_quentity < Quentity_Targets_need)
            {
                // создаем цель \\            
                Generate_Target();
                // создаем помеху\\
                Generate_Interference();
                Report.text = Str_Mistakes = "Появление новой цели," +
                              "\n    кол-во проигранных целей:  " + is_quentity +
                              "\n    кол-во удаленых целей:      " + _quentity_kill +
                              "\n    кол-во ошибок:              " + Mistakes;
                Report.color = Color.black;
                Changed_Text_Button();                
            }            
            // сбрасываем таймер \\           
            timer = 0f;            
        }

        // проходит тест если все цели отработаны \\
        if(Targets.Count == 0 && _quentity_kill == Quentity_Targets_need)
        {
            _hasStarted = false;
            GameManager.Instance.PassCheck();
        }
    }
        

    public void Changed_Text_Button()
    {
        Text text_botton = Target_Buttons[0].GetComponentInChildren<Text>();
        text_botton.text = "Запрос цели";
        //Debug.Log("Смена текста");
    }
    

    public void Remove_Option(int optionIndex)
    {
        // Получаем список всех опций \\
        List<Dropdown.OptionData> options = Choice_target.options;
        // Удаляем опцию с заданным индексом \\
        options.RemoveAt(optionIndex);
        // Обновляем список опций \\
        Choice_target.options = options;        
        Choice_target.value = 0;
    }
    

    public int Find_Free_Number(List<int> numbers)
    {
        numbers.Sort(); // Сортируем список чисел
        int lastNumber = -1; // Предыдущее число из списка

        foreach (int number in numbers)
        {
            if (number - lastNumber > 1)
            {
                // Найдено пропущенное число
                return lastNumber + 1;
            }

            lastNumber = number;
        }

        // Если нет пропущенных чисел, возвращаем следующее число за последним
        return lastNumber + 1;
    }


    public void BrightnessChanged(float value)
    {
        Grid.alpha = value;
    }
    
    
    private bool _close_Iko;
    public void OpenIko()
    {        
        _ikoPanel.alpha = 1.0f;
        _ikoPanel.interactable = true;
        _ikoPanel.blocksRaycasts = true;    
 
        if (InterferenceFolder.transform.childCount > 0)
            Check_Interference_Bloks();

        GameManager.Instance.Reset_Blocks_Action();

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

    // На всякий лень\\
    public void CloseIko()
    {              
        _ikoPanel.alpha = 0.0f;
        _ikoPanel.interactable = false;
        _ikoPanel.blocksRaycasts = false;
    }
    public void Close_IKO()
    {
        Call_Helper("При возвращении на ИКО, \n при наличии помехи будет проверена правильность избавлении от неё.", false);

        if (_hasStarted)
            Stop_Test(true);
       
        Debug.Log("STOP time"); 
        _close_Iko = true;

        GameManager.Instance.Reset_Blocks_Action();
        GameManager.Instance.Clear_Action();
    }

    // Стробирование антенны \\    
    public void Start_Strobing()
    {        
        // Вращение линии \\
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
    
    //проверяем на избавление от помехи\\
    public void Check_Interference_Bloks()
    {
        if (Interferenses_.Count < 1)
            return;

        if (Interferenses_.Peek() == null)
            return;

        Debug.Log("TAG: " + Interferenses_.Peek().gameObject.tag);

        if (!GameManager.Instance.Check_Interference(Interferenses_.Peek().gameObject.tag))
        {
            Mistakes++;
            Report.text = "Ошибка, не правильное избавление от помех, кол-во ошибок: " + Mistakes;
            Report.color = Color.red;
        }
        else
        {
            Report.text = "Правильно, избавление от помех, кол-во ошибок: " + Mistakes;
            Report.color = Color.green;
        }

        Interferenses_.Pop().Check_work = true;
       
        //GameManager.Instance.Clear_Action();
    }


    public void EnableStrobControl()
    {
        _strobContainer.SetActive(true);
    }


    public void DisableStrobControl()
    {
        _strobContainer.SetActive(false);
    }


    public void Generate_Target()
    {
        GameObject target = Instantiate(Target_Flying, TargetsFolder);
        target.transform.SetParent(TargetsFolder.transform);        
        // добавляем ноную цель \\
        is_quentity++;
        // добавляем цель в начало списка \\
        Targets.Insert(0, target);
        // проверка на свободный 00 \\
        if (Targets.Count > 1)
        {
            if(Targets[1].GetComponent<Body_Target>().Namber_on_IKO == 0)
            {
                Report.text = Str_Mistakes = "ТЕСТ НЕ ПРОЙДЕН, ошибка не запросил цель";
                Report.color = Color.red;
                Mistakes = Max_Mistakes;                
            }
        }
        // Получаем существующую коллекцию опций
        List<Dropdown.OptionData> options = Choice_target.options;
        Dropdown.OptionData _target = new Dropdown.OptionData();
        _target.text = "Цель 00";
        options.Insert(0, _target);
        // Устанавливаем обновленную коллекцию опций в Dropdown
        Choice_target.options = options;

    }


    public void Request_Targets()
    {
        if (!_hasStarted) return;
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
                    {
                        Namber_on_IKO.Add(target.GetComponent<Body_Target>().Namber_on_IKO);
                    }
                    int _namber_on_IKO =  Find_Free_Number(Namber_on_IKO);
                    // После запроса цели присваеваем номер и освобождаем 00 \\ 
                    Targets[namber].GetComponent<Body_Target>().Namber_on_IKO = _namber_on_IKO;                    
                    // Изменяем цель в выбор целей \\                
                    Choice_target.options[namber].text = "Цель 0" + _namber_on_IKO;                    
                }
                else
                {
                    // После запроса цели присваеваем номер и освобождаем 00 \\   
                    Targets[namber].GetComponent<Body_Target>().Namber_on_IKO = Targets.Count;
                    // Изменяем цель в выбор целей \\
                    Choice_target.options[namber].text = "Цель 0" + Targets.Count;                  
                }
                // Устанавливаем новое значение без генерации события \\
                Choice_target.SetValueWithoutNotify(namber);                
                Choice_target.GetComponentInChildren<Text>().text = Choice_target.options[namber].text;
                //Choice_target.options.Add(new Dropdown.OptionData() { text = "Цель 0" + Targets.Count });
                if (Targets[namber].GetComponent<Body_Target>().is_Our == true)
                    text_botton.text = "Цель " + namber.ToString() + ", Есть ответ";
                else
                    text_botton.text = "Цель " + namber.ToString() + ", Нет ответа";
            }
            else
            {
                Report.text = Str_Mistakes = "ОШИБКА запрос у запрошенной цели, кол-во ошибок: " + Mistakes;
                Report.color = Color.red;
                Mistakes++;                
            }

        }

    }


    public void Button_Test_Group(bool _is_group)
    { 
        if (!_hasStarted) return;
        int namber = Choice_target.value;
        if (Targets[namber].GetComponent<Body_Target>()._is_group != _is_group)
        {
            Mistakes++;
            Report.text = Str_Mistakes = "ОШИБКА не признал кол-во целей, кол-во ошибок: " + Mistakes;
            Report.color = Color.red;
        }
        else
        {
            Report.text = Str_Mistakes = "ПРАВИЛЬНО кол-во ошибок: " + Mistakes;
            Report.color = Color.green;
            // Цель проверена \\
            Targets[choice_target].GetComponent<Body_Target>().Check_is_Group = true;
        }           

    }
      
    
    public void Button_Test_Our( bool _is_Our)
    {       
        if (!_hasStarted) return;
        int namber = Choice_target.value;
        if(Targets[namber].GetComponent<Body_Target>().Check_Request == true)
        {
            if (Targets[namber].GetComponent<Body_Target>().is_Our != _is_Our)
            {
                Report.text = Str_Mistakes = "ОШИБКА не признал цель, кол-во ошибок: " + Mistakes;
                Report.color = Color.red;
                Mistakes++;
            }
            else
            {
                Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                Report.color = Color.green;
                // Цель проверена \\
                Targets[choice_target].GetComponent<Body_Target>().Check_our = true;
            }
                        
        }       
    }


    public void Restart()
    {
        _hasStarted = false;
        // Удаляем все данные о цели\\
        foreach (var target in Targets)
            if (target != null)
                Destroy(target);
        Targets.Clear();
        is_quentity = _quentity_kill = 0;
        timer = 0f;
        // убераем надписи целей\\
        foreach (var text in Text_targets_fix)
        {
            text.gameObject.SetActive(false);
            text.transform.position = Vector2.zero;
        }
        Choice_target.options.Clear();
        Choice_target.GetComponentInChildren<Text>().text = null;
        // помеху убераем\\        
        while (InterferenceFolder.childCount > 0)
            DestroyImmediate(InterferenceFolder.GetChild(0).gameObject);  
        // линию в начало\\            
        LineObject.transform.localEulerAngles = new Vector3(0, 0, 90);
        StartButton.interactable = true;
        OnReset?.Invoke();
        WorkMode = IkoWorkMode.Rpm6;
        Mistakes = 0;
        Report.text = Str_Mistakes = "";
        Report.color = Color.black;
        // убираем помошнока\\
        _has_help = false;
        Is_Help_Target = false;
        Is_Help_Interference = false;
        Children_Button_Set.gameObject.SetActive(true);
    }
    

    public void Start_Test()
    {
        // start line \\
        if (_hasStarted) return;
        _hasStarted = true;
        // В помощь\\
        if (Status_Target.text == "приказ 66")
            Max_Mistakes = 66;
        StartButton.interactable = false;        
        Report.text = Str_Mistakes = "  НАЧАЛО БОЕВОЕ РАБОТЫ  ";
        Report.color = Color.black;
        Children_Button_Set.gameObject.SetActive(false);
        Call_Helper("Внизу слева будет писаться отчет о прохождении теста", true);
        // создаем цель \\
        Generate_Target();
        // создаем помеху\\
        Generate_Interference();
    }

    

    private bool _stop_antenna;    
    public void Stop_timer()
    {        
        if (!_stop_antenna)
        {
            _stop_antenna = true;
            Report.text = "Пауза";
            Report.color = Color.black;           
            Stop_Test(true);
            Stop_Time.GetComponentInChildren<Text>().text = "Пауза";
            Stop_Time.GetComponent<Image>().color = Color.red;
            Restart_Button.interactable = false;
            StartButton.interactable = false;
        }
        else
        {
            _stop_antenna = false;
            Report.text = "Продолжение";
            Report.color = Color.black;
            if (_hasStarted == false && Targets.Count >= 1)
                Stop_Test(false);                
            Stop_Time.GetComponentInChildren<Text>().text = "Остановить антенну\n(время)";
            Stop_Time.GetComponent<Image>().color = new Color(127 / 255f, 127 / 255f, 127 / 255f);
            Restart_Button.interactable = true;
            StartButton.interactable = true;
        }
        Scrobing_Line.value = 0;
    }
    
        
    private int Find_Azimuth(Vector2 of_Target)
    {
        // Вычисляем угол между векторами \\
        float angle = Vector3.SignedAngle(Vector2.up, of_Target, Vector3.forward);   
        // Корректируем угол, чтобы он был положительным \\
        angle = -1 * angle;
        if (angle < 0)
            angle += 360f;        
        //Debug.Log("Угол: " + angle + " градусов по часовой стрелке");
        return (int)angle;
    }
        
    
    public void Check_line_Targets()
    {
        if (!_hasStarted) return;
        string input = Status_Target.text;        
        string[] numbers = input.Split('-');
        if (numbers.Length != 3)
        {
            Report.text = "Не то что нужно";
            Report.color = Color.blue;
            return;
        }
        int number_target = 0;
        int azimuth = 0; 
        int ring = 0;     
        try
        {
            number_target = int.Parse(numbers[0]);
            azimuth = int.Parse(numbers[1]);
            ring = int.Parse(numbers[2]);
        }
        catch
        {
            Report.text = "Введите числа";
            Report.color = Color.blue;
        }
        Vector2 of_target = Targets[choice_target].GetComponent<Transform>().position;
        
        if(number_target != Targets[choice_target].GetComponent<Body_Target>().Namber_on_IKO)
        {
            Report.text = "Не для той цели значение";
            Report.color = Color.red;
            return;
        }

        bool is_Within_Range(int azimuth_user)
        {
            int azimuth_target = Find_Azimuth(of_target);
            float lowerLimit = azimuth_target * 0.9f; // 90% от нужного числа
            float upperLimit = azimuth_target * 1.1f; // 110% от нужного числа

            return azimuth_user >= lowerLimit && azimuth_user <= upperLimit;
        }

        if (!is_Within_Range(azimuth))
        {
            Report.text = "ОШИБКА Азимута, ожидалось: " + azimuth;
            Report.color = Color.red;
            Mistakes++;
            return;
        }

        float ring_target = (float)(of_target.magnitude * 15 / 4);
        
        if (ring != Mathf.Round(ring_target))
        {
            Report.text = "ОШИБКА СЕКТОРА, правильно: " + Mathf.Round(ring_target);
            Report.color = Color.red;
            Mistakes++;
            return;
        }
        // Цель проверена \\
        Targets[choice_target].GetComponent<Body_Target>().Check_line = true;
        Status_Target.text = "";
        Report.text = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
        Report.color = Color.green;
        return;
    }


    public void Generate_Interference()
    {
        GameObject Interferense;
        //Random.Range(0, 4)
        string _interferense_tag;
        
        switch (Random.Range(0, 4))
        {
            case 0:
                Interferense = Instantiate(Passive_Prefab[Random.Range(0, Passive_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "PASSIVE";
                break;
            case 1:
                Interferense = Instantiate(From_local_Prefab[Random.Range(0, From_local_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "FROM_LOCAL";
                break;
            case 2:
                Interferense = Instantiate(NIP_Prefab[Random.Range(0, NIP_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "NIP";
                break;
            case 3:
                Interferense = Instantiate(Active_noise_Prefab[Random.Range(0, Active_noise_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "ACTIVE_NOISE";
                break;
            default:
                Interferense = Instantiate(Response_Prefab[Random.Range(0, Response_Prefab.Count)], InterferenceFolder);                
                _interferense_tag = "RESPONSE";
                break;
        }

        if(Interferenses_.Count > 0 && Interferenses_.Peek() != null)
        {
            var OLD_interferense = Interferenses_.Peek();
            
            if(OLD_interferense.GetComponent<Body_Interference>().Check_Test == false)
            {
                Mistakes++;
                Report.text = Str_Mistakes = "ОШИБКА, не определил старою помеху";
                Report.color = Color.red;
            }
            
        }

        Interferense.GetComponent<Body_Interference>().tag = _interferense_tag;
        Interferenses_.Push(Interferense.GetComponent<Body_Interference>());

    }

    
    public void Check_Interference(int namber_button)
    {
        if (!_hasStarted) return;
        if (InterferenceFolder.transform.childCount == 0 || Interferenses_.Count == 0)
            return;
              
        Body_Interference interference = Interferenses_.Peek();
        if (interference == null)
            return;

        interference.Check_Test = true;

        switch (namber_button)
        {
            case 0:
                if(interference.tag == "PASSIVE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;                    
                    return;
                }
                break;
            case 1:
                if (interference.tag == "FROM_LOCAL")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;                    
                    return;
                }
                break;
            case 2:
                if (interference.tag == "NIP")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;                    
                    return;
                }
                break;
            case 3:
                if (interference.tag == "ACTIVE_NOISE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;                    
                    return;
                }
                break;
            case 4:
                if (interference.tag == "RESPONSE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;                    
                    return;
                }
                break;            
        }
        Mistakes++;
        Report.text = Str_Mistakes = "ОШИБКА не правильно определии помеху, кол-во ошибок: " + Mistakes;
        Report.color = Color.red;        
    }


    public void Test_children_mode()
    {
        _has_help = true;
        Children_Button_Set.gameObject.SetActive(false);
        Max_Mistakes = 1000;
    }


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