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
    // Список всех появившихся целей на ико \\
    public static List<GameObject> Targets = new List<GameObject>();
    // Список ПРС от целей \\
    public static List<GameObject> PRS_of_target = new List<GameObject>();
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
    private float interval = 30f;
    private float timer = 0f;
    // Выбраная цель\\
    private int choice_target;

    private void Awake() => Instance = this;
  
    void Start()
    {
        // Устанавливаем значение у линии вращение\\
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

        // Проходим по всем целям на ико \\                
        for (int i = 0; i < Targets.Count; i++)
        {
            // Удаляем данные о цели если она уничтожена \\
            if (Targets[i] == null || Targets[i].GetComponent<Body_Target>().End_Player == true)
            {
                Text_targets_fix[i].gameObject.SetActive(false);
                Report.text = Str_Mistakes = "Удаление цели";
                Report.color = Color.black;
                _quentity_kill++;
                // Удаляем номер цели и освобождаем его\\
                Remove_Option(i);
                Targets.RemoveAt(i);                
            }
            else
            {
                // Выбираем цель на ико\\
                if(i == Choice_target.value)
                {
                    choice_target = i;
                    Text_targets_fix[i].color = Color.red;
                    Data_Height.text = "Данные с высотометра: \n" + Targets[i].GetComponent<Body_Target>().Height + " Гектометров";
                }
                else
                    Text_targets_fix[i].color = Color.black;

                // Устанавлимаем номера над целями\\   
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
                // создаем ПРС \\
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = 4.0f;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius;
                Vector2 start_prs = new Vector2(x, y);
                // Вызываем ПРС на ико \\
                if(Random.Range(0, 9) == 1)
                    Generate_PRS(start_prs, 0);
               
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
        // Текст на кнопке меняем\\
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
        // Ищем свободный номер для цель\\
        numbers.Sort(); // Сортируем список чисел
        int lastNumber = -1; // Предыдущее число из списка
        // Найдено пропущенное число
        foreach (int number in numbers)
        {
            if (number - lastNumber > 1)
                return lastNumber + 1;
            lastNumber = number;
        }
        // Если нет пропущенных чисел, возвращаем следующее число за последним
        return lastNumber + 1;
    }

    // Изменяем прозрачность сетки\\
    public void BrightnessChanged(float value) => Grid.alpha = value;  
    
    // Для проверки что окно ико закрыто\\
    private bool _close_Iko;
    public void OpenIko()
    {        
        _ikoPanel.alpha = 1.0f;
        _ikoPanel.interactable = true;
        _ikoPanel.blocksRaycasts = true;    
        // Если есть последавательность для избавленме от помехи то проверяем на избавление\\ 
        if (InterferenceFolder.transform.childCount > 0)
            Check_Interference_Bloks();
        // Очищаем список действий для помехи\\
        GameManager.Instance.Reset_Blocks_Action();
        // Проверяем что ико не на паузе или продолжаем \\
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
        // Проверяем что есть элементы\\
        if (Interferenses_.Count < 1)
            return;
        // Не равны нулю \\
        if (Interferenses_.Peek() == null)
            return;

        Debug.Log("TAG: " + Interferenses_.Peek().gameObject.tag);
        // по Tag оптровляем на проверку на избавление от помехи\\
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
        // Флаг на избавление от помехи\\
        Interferenses_.Pop().Check_work = true;       
        //GameManager.Instance.Clear_Action();
    }

    // Действие с поворотом антенны\\
    public void EnableStrobControl() => _strobContainer.SetActive(true);
    public void DisableStrobControl() => _strobContainer.SetActive(false);
    // Вызиваем новую цель\\    
    public void Generate_Target()
    {
        // Что есть ссылка на префаб\\
        if (Target_Flying == null)
            return;

        GameObject target = Instantiate(Target_Flying, TargetsFolder);
        target.transform.SetParent(TargetsFolder.transform);        
        // добавляем новую цель \\
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
                GameManager.Instance.FailCheck();
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
    // Запрашиваем цель\\
    public void Request_Targets()
    {
        if (!_hasStarted) return;
        // Из списка цель запрашивается которыя первая\\
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
                    text_botton.text = "Цель 0" + Targets[namber].GetComponent<Body_Target>().Namber_on_IKO.ToString() + ", Есть ответ";
                else
                    text_botton.text = "Цель 0" + Targets[namber].GetComponent<Body_Target>().Namber_on_IKO.ToString() + ", Нет ответа";
            }
            else
            {
                Report.text = Str_Mistakes = "ОШИБКА запрос у запрошенной цели, кол-во ошибок: " + Mistakes;
                Report.color = Color.red;
                Mistakes++;                
            }
        }
    }
    // Для теста цели групповая или одиночная\\
    public void Button_Test_Group(bool _is_group)
    { 
        if (!_hasStarted) return;
        // какая на ико выбрана цель \\
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
      
    // Для теста что Свой или Чужой \\
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

    // Заного тест проходить \\
    public void Restart()
    {
        _hasStarted = false;
        // Удаляем все данные о цели\\
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
        Children_Button_Set.gameObject.SetActive(true);
    }
    // Начало теста \\
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
    // Приостановить тест \\
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
       
    // Что значение есть в пределе значений \\
    private bool is_Within_Range(int azimuth_user, Vector2 of_target)
    {
        int azimuth_target = Find_Azimuth(of_target);
        float lowerLimit = azimuth_target * 0.9f; // 90% от нужного числа
        float upperLimit = azimuth_target * 1.1f; // 110% от нужного числа
        return azimuth_user >= lowerLimit && azimuth_user <= upperLimit;
    }
    // Ищем расстояние до цели \\
    private bool Find_Limit(int long_target, float ring)
    {        
        float lowerLimit = (float)((ring - 1) * 10);        
        float upperLimit = (float)((ring + 1) * 10);        
        return long_target * 10 >= (int)lowerLimit && long_target * 10 <= (int)upperLimit;
    }
    // Проверяем доклад о цели \\
    public void Check_line_Targets()
    {
        if (!_hasStarted) return;
        Call_Helper("Для доклада о цели оператор определяет её координаты и докладывает: \n" +
            " «00(сигнал новой цели) - 00(номер цели) - 000 (азимут) - 000 (дальность)»", true);
        string input = Status_Target.text;        
        string[] numbers = input.Split('-');
        if (numbers.Length != 4)
        {
            Report.text = "Не то что нужно, пример 00-00-000-000";
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
            Report.text = "Введите числа, не понятно";
            Report.color = Color.blue;
        }
        // Получем координаты цели \\
        Vector2 of_target = Targets[choice_target].GetComponent<Transform>().position;        
        if(number_target != Targets[choice_target].GetComponent<Body_Target>().Namber_on_IKO)
        {
            Report.text = "Не для той цели значение";
            Report.color = Color.red;
            return;
        }        
        if (!is_Within_Range(azimuth, of_target))
        {
            Report.text = "ОШИБКА Азимута, ожидалось: " + azimuth;
            Report.color = Color.red;
            Mistakes++;
            return;
        }                
        float ring_target = (float)(of_target.magnitude * 15 / 4);
        //ring_to_long != Mathf.Round(ring_target) *10
        if (Find_Limit(ring_to_long, ring_target))
        {
            Report.text = "ОШИБКА ДАЛЬНОСТИ, правильно: " + Mathf.Round(ring_target * 10);
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

    private int exclude = 1;
    // Создаем помеху на ико \\
    public void Generate_Interference()
    {
        GameObject Interferense;
        //Random.Range(0, 4)
        string _interferense_tag;
        List<int> numbers = new() { 0, 1, 2, 3, 4 }; // Создаем список чисел от 0 до 4, за исключением значения 2
        numbers.Remove(exclude); // Удаляем число-исключение из списка
        int randomIndex = Random.Range(0, numbers.Count); // Генерируем случайный индекс из доступных чисел
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
        Call_Helper("На ИКО появилась помеха сначала определить её вид, " +
                "\n потом попробовать избавится от неё выбрав действие в блоках в Машине 1 => Внутри КУНГа." +
                "\n (При неправильном избавлении, помеха все равно исчезает и будет вызвана новая)", true);
        if (Interferenses_.Count > 0 && Interferenses_.Peek() != null)
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
    // Проверяем вид помехи на ико по кнопкам\\
    public void Check_Interference(int namber_button)
    {
        if (!_hasStarted) return;
        // Что они есть на ико\\
        if (InterferenceFolder.transform.childCount == 0 || Interferenses_.Count == 0)
            return;           
        // Получаем последнию помеху на икр\\
        Body_Interference interference = Interferenses_.Peek();
        if (interference == null)
            return;
        interference.Check_Test = true;
        // Проверяем что правильно определил помкху \\
        switch (namber_button)
        {
            case 0:
                if(interference.tag == "PASSIVE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("На ИКО пассивная помеха для избавления нужно установить переключатели на блоках: \n " +
                        "ПОС-71 => Изменить режим работы на стробирование \n " +
                        "ПОВ-72 => в положение ПЕЛЕНГ", true);
                    return;
                }
                break;
            case 1:
                if (interference.tag == "FROM_LOCAL")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("На ИКО местные предметы для избавления нужно установить переключатели на блоках: \n" +
                        "\n К-71 => Местные предметы" +
                        "\n К-71 => Местные предметы" +
                        "\n O-71 => НАЧАЛО ДИСТАНЦИИ" +
                        "\n O-71 => ВХОДНОЕ НАПРЯЖЕНИЕ" +
                        "\n O-71 => МАШТАБ" +
                        "\n O-71 => Штырек" +
                        "\n ПОС-71 => Тумблер Высокое Напряжение", true);
                    return;
                }
                break;
            case 2:
                if (interference.tag == "NIP")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("На ИКО НИП для избавления нужно установить переключатели на блоках: \n" +
                        "\n ФП-71 => ФП ОТКЛ. В положения ФП", true);
                    return;
                }
                break;
            case 3:
                if (interference.tag == "ACTIVE_NOISE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("На ИКО активная шумовая для избавления нужно установить переключатели на блоках: \n" +
                        "\n ПОС-71 => ПЕРЕКЛЮЧЕНИЯ ВОЛН" +
                        "\n ПОВ-72=> ПЕЛЕНГ", true);
                    return;
                }
                break;
            case 4:
                if (interference.tag == "RESPONSE")
                {
                    Report.text = Str_Mistakes = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                    Report.color = Color.green;
                    Call_Helper("На ИКО ответная помеха для избавления нужно установить переключатели на блоках: \n" +
                        "\n ФП-71 => ФП ОТКЛ. В положения ФП", true);
                    return;
                }
                break;            
        }
        Mistakes++;
        Report.text = Str_Mistakes = "ОШИБКА не правильно определии помеху, кол-во ошибок: " + Mistakes;
        Report.color = Color.red;        
    }
    // Создаем ПРС \\
    public void Generate_PRS(Vector2 start_PRS , int of_target)
    {       
        // Только одна ПРСможет быть на ико\\
        if (PRS_of_target.Count >= 2)
            return;
        if (of_target == 0)
            Call_Helper("На ИКО противорадиолокационный снаряд. Доложить: пуск ПРС 000-000 (азимут-дальность)" +
                " \n и избавиться от него на ПОС72 => Вскрыть крышку и включаеть режим мерцания ", true);
        else
            Call_Helper("На ИКО ПротивоРадиолокационный Снаряд. Доложить: Цель 00, пуск ПРС 000-000 (азимут-дальность)" +
                "\n и избавиться от него на ПОС72 => Вскрыть крышку и включить режим мерцания ", true);
        // Создаем ПРС\\
        GameObject PRS = Instantiate(PRS_target, TargetsFolder, false);
        PRS.transform.SetParent(TargetsFolder.transform);
        PRS.GetComponent<Body_PRS>().Start_PRS = start_PRS;
        PRS.GetComponent<Body_PRS>().Of_Target = of_target;
        // Добовляем в список  \\
        PRS_of_target.Add(PRS);      
    }
    // Проверяем что провильно долошил о ПРС\\
    public void Check_Line_PRS()
    {
        // Получаем данные из строки\\
        //Цель 00, пуск ПРС 000-000
        string input = Status_PRS.text;
        List<int> data_report = new List<int>();
        string[] words = input.Split(' ', ',', '-');        
        foreach (string word in words)
        {
            int num;
            if (int.TryParse(word, out num))
                data_report.Add(num);         
        }
        // Получили Введеные данные\\
        int number_target = 0;
        int azimuth;
        int ring_to_long;
        // пуск ПРС от цели или одиночная \\
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
            Report.text = "Не то что нужно, пример: Цель 00, пуск ПРС 000-000 или" +
                                                "\n Пуск ПРС 000-000";
            Report.color = Color.blue;
            return;
        }

        Debug.Log("number_target: " + number_target + " azimuth: " + azimuth + " ring_to_long: " + ring_to_long);
        Debug.Log("PRS_of_target.Count: " + PRS_of_target.Count);                
        // Проверяем данные о цели \\
        foreach (var prs in  PRS_of_target)
        {            
            if(prs != null && prs.GetComponent<Body_PRS>().Of_Target == number_target)
            {
                Vector2 position_prs = prs.GetComponent<Transform>().position;

                Debug.Log("position_prs: " + position_prs);

                if (!is_Within_Range(azimuth, position_prs))
                {
                    Report.text = "ОШИБКА Азимута, ожидалось: " + azimuth;
                    Report.color = Color.red;
                    Mistakes++;
                    return;
                }

                float ring_target = (float)(position_prs.magnitude * 15 / 4);
                //ring_to_long != Mathf.Round(ring_target) *10
                if (Find_Limit(ring_to_long, ring_target))
                {
                    Report.text = "ОШИБКА ДАЛЬНОСТИ, правильно: " + Mathf.Round(ring_target * 10);
                    Report.color = Color.red;
                    Mistakes++;
                    return;
                }

                prs.GetComponent<Body_PRS>().Check_Status = true;
                Status_PRS.text = "Цель , пуск ПРС ";

                Report.text = "ПРАВИЛЬНО, кол-во ошибок: " + Mistakes;
                Report.color = Color.green;

                return;
            } 
        }
        Report.text = "Не для той цели значение";
        Report.color = Color.red;
    }
    // Проверка режима мерцание у ПРС что установлен\\ 
    public void Check_Flickering(GameObject PRS)
    {
        if (!PRS.GetComponent<Body_PRS>().Mode_Frickering)
            Kill_RLS();
        // Удаляем ПРС == Промазать \\
        PRS_of_target.Remove(PRS);
        Destroy(PRS);        
        Report.text = Str_Mistakes = "противорадиолокационный снаряд => промaзать";
        Report.color = Color.green;
    }
    // Уничтожение РЛС ПРСом \\
    private void Kill_RLS()
    {
        // тест проволен РЛС уничножена\\
        if (Random.Range(0, 1) == 1)
            return;

        _hasStarted = false;
        Report.text = Str_Mistakes = "РЛС была уничтожена противорадиолокационным снарядом";
        Report.color = Color.red;        
        GameManager.Instance.FailCheck();
    }

    // Установика что включен режим мерцание у ПРС \\
    public void Set_Mode_Frickering()
    {
        foreach (var prs in PRS_of_target)
            if(prs != null)
                prs.GetComponent<Body_PRS>().Mode_Frickering = true;
    }

    // Установка учебного режима\\
    public void Test_children_mode()
    {
        _has_help = true;
        Children_Button_Set.gameObject.SetActive(false);
        Max_Mistakes = 1000;
    }
        
    // Вызвать учебный режим\\
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