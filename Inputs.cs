using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Text = TMPro.TextMeshProUGUI;

using Common;
using Common.Extensions;

[Serializable]
public struct TimeData
{
    public DateTime _time;
    public DateTime Time
    {
        get
        {
            DateTime time = _time, now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
        }
        set
        {
            var time = value;
            _time = new DateTime(0, 0, 0, time.Hour, time.Minute, time.Second);
        }
    }
    public static implicit operator TimeData(DateTime d) => new(d);
    public static implicit operator TimeData(TimeSpan d) => new(DateTime.Now.YMD() + d);
    public TimeData(DateTime d)
    {
        _time = d;
    }
    public TimeData(int hour, int minute)
    {
        DateTime now = DateTime.Now;
        Debug.Log(hour + ", " + minute);
        _time = new(now.Year, now.Month, now.Day, (hour + minute / 60) % 24, minute % 60, 0);
    }
}
public class InputDescription {
    private readonly string _caption;
    public string Caption => _caption;
    private readonly object[] _params;
    public object[] Parameters => _params;
    public InputDescription(string caption) {
        _caption = caption;
        _params = null;
    }
    public InputDescription(string caption, params object[] @params) : this(caption) {
        this._params = @params;
    }
    public static implicit operator InputDescription(string s) => new(s);
}
public class Screen
{
    public string caption;
    public override string ToString()
    {
        return caption;
    }
    public object this[string name]
    {
        get => Values[name];
        set => Values[name] = value;
    }
    public Dictionary<string, Type> Types = new Dictionary<string, Type>();
    private Dictionary<string, object> Values;
    private Dictionary<string, InputDescription> _ivmParams;
    private Dictionary<string, InputValueManager> map;
    public T GetInputManager<T>(string w) where T : InputValueManager 
    {
        return map[w] as T;
    }
    public bool backed;
    public bool CanSubmit => Values.All(p => p.Value != null);
    public bool Unloaded => !Inputs.Screens.Contains(this);
    public bool Displayed => Inputs.Screens.TryPeek(out Screen screen) && screen == this;
    public Screen(string caption, Dictionary<InputDescription, Type> inputs)
    {
        this.caption = caption;

        _ivmParams = inputs.Keys.ToDictionary(k => k.Caption, v => v);
        foreach (var pair in inputs) {
            Types[pair.Key.Caption] = inputs[pair.Key];
        }

        Values = new Dictionary<string, object>();
        map = new();

        foreach (var k in Types.Keys)
        {
            Values[k] = default;
        }
    }
    public void Initialize(Dictionary<string, InputValueManager> map)
    {
        foreach (var pair in Values)
        {
            map[pair.Key].Init(_ivmParams[pair.Key].Parameters);
            map[pair.Key].Fill(pair.Value);
            this.map[pair.Key] = map[pair.Key];
        }
    }
}
public class Inputs : MonoBehaviour
{
    // [NOTE]
    // when using screens, persistent screens (screens stored in the below dictionary) should only be used for (1) nested screens, in order to store data across screens and/or (2) large screens (i.e. more than 2 parameters)
    // otherwise it's pointless and you might as well create a billion ui screens in the editor at that point and by that measure

    public static Dictionary<string, Screen> ScreensDictionary = new()
    {
        { "New Assignment Due Date",  new Screen("New Assignment Due Date",
            new()
            {
                { "DateTime", typeof(DateTime) }
            }) },
        { "New Assignment",  new Screen("New Assignment",
            new()
            {
                { "Title", typeof(string) },
                { "Start Date (optional)", typeof(DateTime?) },
                { "End Date", typeof(DateTime) },
                { "Set Date Relatively", typeof(Decision) },
                { "Note", typeof(LongString) }
            }) },
        { "New Assignment (R)",  new Screen("New Assignment",
            new()
            {
                { "Title", typeof(string) },
                { "Start Time Delay", typeof(TimeSpan) },
                { "End Time Delay", typeof(TimeSpan) },
                { "Set Date Directly", typeof(Decision) },
                { "Note", typeof(LongString) }
            }) },
        { "New Persistent Assignment",  new Screen("New Persistent Assignment",
            new()
            {
                { "Title", typeof(string) },
                { "Delay After Completion", typeof(TimeSpan) },
                { "Start Date", typeof(DateTime) },
                { "End Date", typeof(DateTime) },
                { "Note", typeof(LongString) }
            }) },
        { "New Task",  new Screen("New Task",
            new()
            {
                { "Title", typeof(string) },
                { "Start Time", typeof(TimeData) },
                { "End Time", typeof(TimeData) },
                { "Note", typeof(LongString) }
            }) },
        { "New Random Assignment",  new Screen("New Random Assignment",
            new()
            {
                { "Title", typeof(string) },
                { "Start Date (optional)", typeof(DateTime?) },
                { "End Date", typeof(DateTime) },
                { "Minimum Random Alert Delay", typeof(TimeSpan) },
                { "Maximum Random Alert Delay", typeof(TimeSpan) },
                { "Note", typeof(LongString) }
            }) },
        { "New Reminder", new Screen("New Reminder", 
            new() 
            {
                { "Title", typeof(string) },
                { "Date", typeof(DateTime) },
                { "Set Date Relatively", typeof(Decision) },
                { "Note", typeof(LongString) }
            }) },
        { "New Reminder (R)", new Screen("New Reminder", 
            new() 
            {
                { "Title", typeof(string) },
                { "Delay", typeof(TimeSpan) },
                { "Note", typeof(LongString) }
            }) },
        { "New Persistent Reminder", new Screen("New Persistent Reminder",
            new() 
            {
                { "Title", typeof(string) },
                { "Delay After Completion", typeof(TimeSpan) },
                { "Date", typeof(DateTime) },
                { "Note", typeof(LongString) }
            }) },
        { "New Note", new Screen("New Note",
            new()
            {
                { "Title", typeof(string) },
                { "Description", typeof(string) },
                { "Note", typeof(LongString) }
            }) },
        { "Set Assignment Type",  new Screen("Set Assignment Type",
            new()
            {
                { "Assignment Type", typeof(AssignmentType) },
            }) },
        { "Random Alert Frequency", new Screen("Random Alert Frequency",
            new() {
                { "Minimum Delay", typeof(TimeSpan) },
                { "Maximum Delay", typeof(TimeSpan) }
            }) },
        { "See Note", new Screen("See Note",
            new() {
                { "Note", typeof(LongString) }
            }) }
    };
    public static Inputs Instance;
    public Text caption;
    public Transform inputs;
    private Transform _submit, _back;
    private void Awake()
    {
        Instance = this;
        _lastInputs = new();

        _submit = inputs.Find("Submit");
        _back = inputs.Find("Back");

        _inputPrefabs = new();
        _InitializePrefabs();

        Screens = new();
        OnSubmit = new();
        OnBack = new();
        StartReadScreens();
    }
    private static readonly Type[] _SupportedTypes = new[] { typeof(string), typeof(bool), typeof(DateTime), typeof(DateTime?), typeof(TimeSpan), typeof(AssignmentType), typeof(Assignment), typeof(TimeData), typeof(Decision), typeof(LongString) };
    #region Input Methods
    private static Dictionary<Type, GameObject> _inputPrefabs;
    private void _InitializePrefabs()
    {
        for (int i = 0; i < _SupportedTypes.Length; i++)
        {
            var type = _SupportedTypes[i];
            _inputPrefabs[type] = Resources.Load<GameObject>("Inputs/" + type + "Input");
            _inputPrefabs[type].GetComponent<InputValueManager>().submit = _submit.GetComponent<Button>();
        }
    }
    private static Dictionary<Type, List<object>> _lastInputs;
    public static void RecordInput<T>(object input)
    {
        var type = typeof(T);
        if (_lastInputs.ContainsKey(type))
        {
            _lastInputs[type].Add(input);
        }
        else
        {
            _lastInputs[type] = new() { input };
        }
    }
    public static void ClearInputs(Type type)
    {
        _lastInputs[type] = new();
    }
    public static void ClearInputs<T>()
    {
        var type = typeof(T);
        _lastInputs[type] = new();
    }
    public static void RecordInputs<T>(params object[] inputs)
    {
        var type = typeof(T);
        _lastInputs[type].AddRange(inputs);
    }
    public static List<T> GetInputs<T>()
    {
        return _lastInputs[typeof(T)].ConvertAll(c => (T)c);
    }
    public static List<object> GetInputs(Type type)
    {
        return _lastInputs[type];
    }
    public static T GetLastInput<T>()
    {
        return (T)_lastInputs[typeof(T)][^1];
    }
    public static object GetLastInput(Type type)
    {
        return _lastInputs[type][^1];
    }
    public static bool AnyInput<T>()
    {
        var type = typeof(T);
        return _lastInputs.ContainsKey(type) && _lastInputs[type].Count > 0;
    }
    
    private void _DestroyAllInputs()
    {
        for (int i = 0; i < inputs.childCount - 2; i++) // last one will be the submit button
        {
            Destroy(inputs.GetChild(i).gameObject);
        }
    }
    private InputValueManager _InstantiateInput<T>(string caption)
    {
        var type = typeof(T);
        var manager = Instantiate(_inputPrefabs[type], inputs).GetComponent<InputValueManager>();
        manager.caption.text = caption;
        return manager;
    }
    private InputValueManager _InstantiateInput(Type type, string caption)
    {
        var manager = Instantiate(_inputPrefabs[type], inputs).GetComponent<InputValueManager>();
        manager.caption.text = caption;
        return manager;
    }
    private InputValueManager _InstantiateInput(Type type)
    {
        var manager = Instantiate(_inputPrefabs[type], inputs).GetComponent<InputValueManager>();
        manager.caption.text = "";
        return manager;
    }
    #endregion
    #region Screen Structure
    public Transform InputScreens;
    private static readonly Func<Screen, bool> DefaultOnBackCallback = screen => false;
    // On back returns true if destroy all screens and return to a main screen.
    public static void LoadScreen(Screen scrn, Action<Screen> onSubmit, Func<Screen, bool> onBack)
    {
        print(Screens);
        Screens.Push(scrn);
        OnSubmit.Add((scrn, onSubmit));
        OnBack.Add(onBack ?? DefaultOnBackCallback);
    }
    private void _FixButtonsInHierarchy()
    {
        _submit.SetAsLastSibling();
        _back.SetAsLastSibling();
    }
    private Dictionary<string, InputValueManager> BuildScreen(Screen scrn)
    {
        scrn.backed = false;
        Instance.caption.text = scrn.caption;

        InputScreens.IsolateSelf(); // disable Main object, enable InputScreens

        Instance._DestroyAllInputs();
        Dictionary<string, InputValueManager> map = new Dictionary<string, InputValueManager>();
        foreach (KeyValuePair<string, Type> request in scrn.Types)
        {
            ClearInputs(request.Value);

            var input = Instance._InstantiateInput(request.Value, request.Key);
            map.Add(request.Key, input);
            input.AssignScreenKeyPair(scrn, request.Key);
        }
        Instance._FixButtonsInHierarchy();
        return map;
    }

    public static Stack<Screen> Screens;
    // always runs

    // Screens use placeholders to store data
    // Placeholders are modified by the user and changes are saved
    // Placeholders can also be modified by other screens
    // <Placeholder> - reference to Screen, string key of Placeholder data for screen

    //
    //
    // can "plug" into each other
    // when screens are "plugged in" they send data once they are fully assigned
    // reference to screen, list of keys to which data is sent
    // e.g.

    // (S) Add Alarm --> (B) Add Assignment --> (S) Add Assignment --> (S) Add Alarm [(B) Add Assignment is set]
    // Here (B) Add Assignment sends to (S) Add Assignment a list of keys:
    // Assignment: 
    // Assignment is the only key attached. Assignment is a key in the data of the (S) Add Alarm screen as well.
    // When Assignment is set in (S) Add Assignment, (S) Add Alarm has Assignment placeholder set to last input (from (S) Add Assignment)
    public static List<(Screen screen, Action<Screen> func)> OnSubmit;
    // return true if screen was correct
    public static List<Func<Screen, bool>> OnBack; 
    // return true if back destroys the current screen
    public static event System.Action OnLoad;
    private static void _OnSubmit(Screen popped)
    {
        for (int i = 0; i < OnSubmit.Count; i++)
        {
            if (OnSubmit[i].screen == popped)
            {
                OnSubmit[i].func?.Invoke(popped);
                OnSubmit.RemoveAt(i--);
            }
        }
    }
    private static bool _OnBack(Screen popped) 
    {
        bool destroy = false;
        popped.backed = true;
        for (int i = 0; i < OnBack.Count; i++) 
        {
            if (destroy = OnBack[i](popped)) 
            {
                OnBack.RemoveAt(i--);
            }
        }
        return destroy;
    }

    private Screen Current;
    public static event Action SaveVariables;

    public int count;
    private Coroutine StartReadScreens()
    {
        void ScreenListener()
        {
            SaveVariables();
            // set current screen's referenced values
            _OnSubmit(Screens.Pop());
            // pop current screen from stack
            if (Screens.Count == 0)
            {
                print("ummm... awkward!");
                Main.ShowMainScreen(); // enable main
            }
        };
        void BackListener()
        {
            // pop current screen from stack
            var screen = Screens.Pop();
            // clear onsubmit handlers of previous screen
            for (int i = 0; i < OnSubmit.Count; i++) {
                if (OnSubmit[i].screen == screen) {
                    OnSubmit.RemoveAt(i--);
                }
            }
            if (_OnBack(screen) || Screens.Count == 0)
            {
                Screens.Clear(); // destroy all screens
                Main.ShowMainScreen(); // enable main
            }
        };
        IEnumerator _IEnum()
        {
            Button submit = _submit.GetComponent<Button>(), back = _back.GetComponent<Button>();
            Button.ButtonClickedEvent onSubmit = submit.onClick, onBack = back.onClick;
            onSubmit.AddListener(ScreenListener);
            onBack.AddListener(BackListener);

            Current = null;
            while (true)
            {
                if (Screens.TryPeek(out var screen)) // if there is a screen in the stack
                {
                    print("COUNT " + Screens.Count);
                    Main.DisableUpdate();
                    if (screen != Current) // if that screen in the stack is not what we are already looking at 
                    {
                        print("985-022");
                        Current = screen; // set screen we are looking at to that screen
                        onSubmit.RemoveListener(ScreenListener);
                        var map = BuildScreen(Current); // build the current screen
                        Current.Initialize(map); // fill placeholders and call init on all input value managers 
                        
                        OnLoad?.Invoke();
                        OnLoad = null;
                        
                        onSubmit.AddListener(ScreenListener); // readd screen listeneer
                    }
                }
                else // if there is no screen in the stack
                {
                    Current = null;
                    Main.EnableUpdate();
                    yield return new WaitUntil(() => Screens.TryPeek(out _)); // wait until there is a screen in the stack
                }
                yield return null;
            }
        }
        return CommonGameManager.Coroutine(_IEnum());
    }
    #endregion
    ///<summary>
    /// Deprecated. The Screen Structure is now used instead.
    ///</summary>
    #region Await Values
    public static IEnumerator AwaitValues(string caption, Screen screen)
    {
        Main.DisableUpdate();
        Instance.caption.text = caption;

        bool _assign = false;
        Main.Instance.transform.IsolateChild(1); // disable Main object, enable InputScreens

        Instance._DestroyAllInputs();
        foreach (KeyValuePair<string, Type> request in screen.Types)
        {
            ClearInputs(request.Value);
            Instance._InstantiateInput(request.Value, request.Key);
        }
        Instance._FixButtonsInHierarchy();

        var submit = Instance._submit.GetComponent<Button>();
        submit.onClick.AddListener(() =>
        {
            _assign = true;
        });

        yield return new WaitUntil(() => _assign);
        Main.EnableUpdate();
    }
    public static IEnumerator AwaitValues(string caption, Dictionary<string, Type> requests)
    {
        Main.DisableUpdate();
        Instance.caption.text = caption;

        bool _assign = false;
        Main.Instance.transform.IsolateChild(1); // disable Main object, enable InputScreens

        Instance._DestroyAllInputs();
        foreach (KeyValuePair<string, Type> request in requests)
        {
            ClearInputs(request.Value);
            Instance._InstantiateInput(request.Value, request.Key);
        }
        Instance._FixButtonsInHierarchy();

        var submit = Instance._submit.GetComponent<Button>();
        submit.onClick.AddListener(() =>
        {
            _assign = true;
        });

        yield return new WaitUntil(() => _assign);
        Main.EnableUpdate();
    }
    /*public static Coroutine LoadScreen(string nm)
    {
        Screens.Push(ScreensByName[nm]);
    }*/
    public static IEnumerator AwaitValues(string caption, params Type[] requests)
    {
        Main.DisableUpdate();
        Instance.caption.text = caption;

        bool _assign = false;
        Main.Instance.transform.IsolateChild(1); // disable Main object, enable InputScreens

        Instance._DestroyAllInputs();
        foreach (Type request in requests)
        {
            ClearInputs(request);
            Instance._InstantiateInput(request);
        }
        Instance._FixButtonsInHierarchy();

        var submit = Instance._submit.GetComponent<Button>();
        submit.onClick.AddListener(() =>
        {
            _assign = true;
        });

        yield return new WaitUntil(() => _assign);
        Main.EnableUpdate();
        // do not need to reenable--script attached to button in hierarchy
    }
    #endregion
}

public class Decision
{
    public Action action;
}