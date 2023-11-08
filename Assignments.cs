using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

using Text = TMPro.TextMeshProUGUI;

using Common;
using Common.Helpers;
using Common.Extensions;

using Unity.Mathematics;

public enum AssignmentType
{
    Normal,
    Persistent,
    Task,
    Random, // Triggered only by Random Alert
    Reminder,
    PersistentReminder,
    ContinuousRandom,
    Note,
    Count
}

[Serializable]
public abstract class Assignment
{
    public string note;
    private bool _done;
    public virtual bool RunWhenDone => false;
    public bool IsDone { get => _done; }
    public void ToggleDone()
    {
        _done = !_done;
        Assignments.Rebuild();
    }
    public void SetDone(bool d) {
        if (d == _done) {
            return;
        } ToggleDone();
    }

    private string _title;
    private DateTime? _startDate;
    private  DateTime _dueDate;
    public Assignment(string title) 
    {
        _title = title;
    }
    public Assignment(string title, DateTime? startDate, DateTime dueDate, string note)
    {
        _title = title;
        SetStartDate(startDate);
        SetDueDate(dueDate);
        this.note = note;
    }
    public string Title { get => _title; }
    public void SetTitle(string t)
    {
        _title = t;
        Assignments.Rebuild();
    }

    public virtual DateTime DueDate => _dueDate;

    public bool HasStartDate => _startDate != null; // if start date is assigned
    public virtual DateTime StartDate => _startDate.Value;
    public DateTime GenericStartDate => HasStartDate ? StartDate : DueDate;
    public void SetDueDate(int year, int month, int day, int hour, int minute)
    {
        _dueDate = new DateTime(year, month, day, hour, minute, 0); // dont need seconds
        Assignments.Rebuild();
    }
    public virtual void SetDueDate(DateTime dateTime)
    {
        _dueDate = dateTime;
        Assignments.Rebuild();
    }
    public virtual void SetStartDate(DateTime? startTime)
    {
        _startDate = startTime;
        Assignments.Rebuild();
    }
}
[Serializable]
public class NormalAssignment : Assignment
{
    public NormalAssignment(string title, DateTime? startDate, DateTime dueDate, string note)
    : base(title, startDate, dueDate, note)
    {

    }
}

// TODO: ask: should persistent assignments increment from the time they start/due or from when they are completed?
[Serializable]
public class PersistentAssignment : Assignment
{
    public override bool RunWhenDone => true;
    private TimeSpan _completedIncrement;
    public TimeSpan Increment
    {
        get => _completedIncrement;
        set => _completedIncrement = value;
    }
    public PersistentAssignment(string title, TimeSpan timeIncrement, TimeData startDate, TimeData dueDate, string note)
    : base(title, startDate.Time, dueDate.Time, note)
    {
        Debug.Log(timeIncrement);
        _completedIncrement = timeIncrement;
    }
    public void MarkCompleted(DateTime currentTime)
    {
        // SetStartDate(DateTime.Now.YMD() + StartTime);
        if (_completedIncrement != default)
        {
            Debug.Log(_completedIncrement);

            int incrementTimes;
            incrementTimes = Mathf.CeilToInt((currentTime - DueDate).Ticks / _completedIncrement.Ticks) + 1;
            Debug.Log(incrementTimes);
            base.SetStartDate(StartDate + _completedIncrement * incrementTimes);
            base.SetDueDate(DueDate + _completedIncrement * incrementTimes);
        }
        else
        {
            SetDone(false);
        }
    }
    public void UnmarkCompleted()
    {
        // SetStartDate(DateTime.Now.YMD() + StartTime);
        base.SetDueDate(DateTime.Now.YMD() + _completedIncrement);
    }
}
[Serializable]
public class Task : Assignment 
{
    public override bool RunWhenDone => true;
    // Tasks are different to assignments in two ways:
        // 1. Tasks are persistent, meaning they are based on time and not on datetime
        // 2. Tasks do not alert when they are set *before* the current time
        // 3. Tasks can be placed in Task Groups, where their times are made to fit between a range of time
        // 4. Tasks mark themselves as done automatically when they are "completed" (when their due dates are passed)
    public int timesCompleted;
    public TimeSpan DueTime => base.DueDate.TimeOfDay;
    public TimeSpan StartTime => base.StartDate.TimeOfDay;
    public override DateTime DueDate => base.DueDate;
    public Task(string title, TimeData startDate, TimeData dueDate, string note) : base(title)
    {
        Debug.Log("TSKSTRT: " + startDate.Time);
        Debug.Log("TSKEND: " + dueDate.Time);
        
        SetStartDate(startDate.Time);
        ChecksetDueDate(dueDate.Time);

        Debug.Log("TSKSTRT': " + StartDate);
        Debug.Log("TSKEND': " + DueDate);

        this.note = note;
    }
    ///<summary>
    /// Takes its TimeOfDay and attempts to reassign
    ///</summary>
    private void ChecksetDueDate(TimeData time)
    {
        var now = DateTime.Now;

        TimeSpan dueTime = time.Time.TimeOfDay, startTime = StartTime;
        DateTime due = default, start = default;
        if (startTime < dueTime) {
            if (now > time.Time) {
                due = DateTime.Now.YMD() + dueTime + TimeSpan.FromDays(timesCompleted);
                start = DateTime.Now.YMD() + startTime + TimeSpan.FromDays(timesCompleted);
            } else {
                due = DateTime.Now.YMD() + dueTime;
                start = DateTime.Now.YMD() + startTime;
            }
        } else { // if startTime is ahead of dueTime, startTime should be for today but dueTime
        // should be for the following day
            due = DateTime.Now.YMD() + dueTime + TimeSpan.FromDays(1); // - TimeSpan.FromDays(1);
            start = DateTime.Now.YMD() + startTime; // - TimeSpan.FromDays(1);
        }
        
        SetStartDate(start);
        base.SetDueDate(due);
    }
    public override void SetDueDate(DateTime dateTime)
    {
        ChecksetDueDate(dateTime);
    }
    public void MarkCompleted() 
    {
        TimeSpan st = StartTime, dt = DueTime;
        // SetStartDate(DateTime.Now.YMD() + StartTime);
        timesCompleted++;
        ChecksetDueDate(dt);
    }
}
[Serializable]
public class RandomAssignment : Assignment
{
    private TimeSpan _min, _max;
    public TimeSpan MinAlarmDelay => _min;
    public TimeSpan MaxAlarmDelay => _max;
    private DateTime _notificationDate;
    public DateTime NextAlarmDate => _notificationDate;
    public bool IsPastAlarmDate => NextAlarmDate >= DateTime.Now;
    public void SetNextAlarmDate()
    {
        _notificationDate += GetNextDelay();
        if (_notificationDate > DueDate)
        {
            _notificationDate = DueDate;
        }
    }
    public RandomAssignment(Assignment a, TimeSpan min, TimeSpan max, string note) 
         : base(a.Title, a.HasStartDate ? a.StartDate : null, a.DueDate, note)
    {
        _min = min;
        _max = max;
        _notificationDate = a.HasStartDate ? a.StartDate + GetNextDelay() : DateTime.Now + GetNextDelay();
        SetNextAlarmDate();
    }
    public RandomAssignment(string title, DateTime? startDate, DateTime dueDate, string note) 
         : base(title, startDate, dueDate, note)
    {
    }
    public TimeSpan GetNextDelay() => UnityEngine.Random.value * (_max - _min) + _min;
    public void SetMinPause(TimeSpan _newMin) => _min = _newMin;
    public void SetMaxPause(TimeSpan _newMax) => _min = _newMax;
}
[Serializable]
public class Reminder : Assignment 
{
    public Reminder(string title, DateTime date, string note) : base(title, null, date, note)
    {
        
    }
}
[Serializable]
public class PersistentReminder : Reminder 
{
    public override bool RunWhenDone => true;
    private TimeSpan _completedIncrement;
    public TimeSpan Increment {        
     get => _completedIncrement;
     set => _completedIncrement = value;
    }
    public PersistentReminder(string title, DateTime date, TimeSpan increment, string note) : base(title, date, note)
    { 
        _completedIncrement = increment;
    }
    
    public void MarkCompleted() 
    {
        // SetStartDate(DateTime.Now.YMD() + StartTime);
        while (DueDate <= DateTime.Now) {
            base.SetDueDate(DueDate + _completedIncrement);
        }
    }
    public void UnmarkCompleted() 
    {
        // SetStartDate(DateTime.Now.YMD() + StartTime);
        base.SetDueDate(DateTime.Now.YMD() + _completedIncrement);
    }
}
[Serializable]
public class Note : Assignment
{
    public Note(string title, string description) : base(title, null, default, null) { this.description = description; }
    private string description;
    public string Description => description;
}
public class ContinuousRandom : Assignment
{
    private TimeSpan _min, _max;
    public TimeSpan MinAlarmDelay => _min;
    public TimeSpan MaxAlarmDelay => _max;
    private DateTime _notificationDate;
    public DateTime NextAlarmDate => _notificationDate;
    public bool IsPastAlarmDate => NextAlarmDate >= DateTime.Now;
    public void SetNextAlarmDate()
    {
        _notificationDate += GetNextDelay();
        if (_notificationDate > DueDate)
        {
            _notificationDate = DueDate;
        }
    }
    public ContinuousRandom(string title, string description, DateTime start, string note) : base(title, start, default, note) 
    {
        
    }
    public TimeSpan GetNextDelay() => UnityEngine.Random.value * (_max - _min) + _min;
    public void SetMinPause(TimeSpan _newMin) => _min = _newMin;
    public void SetMaxPause(TimeSpan _newMax) => _min = _newMax;
}
//TODO: Assignment groups to prevent/solve assignment date conflicts
// Could be through the use of group ids
// Could show only one kind of group as an option (Display only group: )


// SUGGESTION: take a modular approach to assignments
// assignments can have due date and start date enabled/disabled,
// assignments can be enabled as candidates for random alerts
// assignments can have random alerts

// SUGGESTION: Have assignments have the Assignment component
// instead of managing LITERALLY EVERYTHING from assignments
// and main. That way it would be easier to manage, likely
// less buggy and could still support multiple modes of 
// functionality

public class Assignments : MonoBehaviour
{
    // TODO: MAKE PENDING LIST PAGE-BASED INSTEAD OF SCROLLED

    public static Assignments Instance;
    public static List<Assignment> FocusedAssignments => Groups.focusedGroup.assignments;
    private List<List<Assignment>> _assignments;
    public static void PushToTop(int index)
    {
        var assignments = FocusedAssignments;
        Assignment aI = assignments[index];
        assignments.RemoveAt(index);
        assignments.Insert(0, aI);
        Rebuild();
    }
    public static int NumberOfAssignments => Instance._assignments.Sum(a => a.Count);
    private static readonly Color _HighlightedColor = Color.green, _DefaultColor = Color.white;
    public static Assignment[] GetAssignments() 
    {
        List<Assignment> assignments = new();
        foreach (var a in Instance._assignments) {
            assignments.AddRange(a);
        }
        return assignments.ToArray();
    }
    
    /*/// <summary>
    /// Method should only be used with saving/loading. dont fuck around
    /// </summary>
    /// <param name="assignments"></param>
    public static void LoadAssignments(Assignment[] assignments)
    {
        Instance._assignments = new(assignments);
        Rebuild();
    }*/
    public static void Reaccumulate()
    {
        Instance._assignments.Clear();
        foreach (var group in Groups.Instance.groups)
        {
            Instance._assignments.Add(group.assignments);
        }
    }
    public static bool AnyAssignments()
    {
        return FocusedAssignments.Count > 0 && FocusedAssignments.Any(a => !a.IsDone);
    }
    public static void HighlightAssignment(int index)
    {
        if (index >= FocusedAssignments.Count)
        {
            Debug.LogError("out of bounds encountered for index " + index, Instance);
            return;
        }
        var assignment = FocusedAssignments[index];
        Transform assignmentTransform;
        if (assignment.IsDone)
        {
            assignmentTransform = Instance.CompletedAssignments.Find(nameof(Assignment) + (index + 1));
        }
        else
        {
            assignmentTransform = Instance.PendingAssignments.Find(nameof(Assignment) + (index + 1));
        }
        var images = assignmentTransform.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            image.color = _HighlightedColor;
        }
    }
    public static void UnhighlightAssignment(int index)
    {
        if (index >= FocusedAssignments.Count)
        {
            Debug.LogError("out of bounds encountered for index " + index);
            return;
        }
        var assignment = FocusedAssignments[index];
        Transform assignmentTransform;
        if (assignment.IsDone)
        {
            assignmentTransform = Instance.CompletedAssignments.Find(nameof(Assignment) + (index + 1));
        }
        else
        {
            assignmentTransform = Instance.PendingAssignments.Find(nameof(Assignment) + (index + 1));
        }
        var images = assignmentTransform.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            image.color = _DefaultColor;
        }
    }
    public static event Action OnAssignmentsRebuilt;
    public static event Action<Assignment> OnRemoveAssignment;
    public static void RemoveAssignment(Group group, int index)
    {
        var assignment = group.assignments[index];
        group.assignments.RemoveAt(index);
        OnRemoveAssignment(assignment);
    }
    public static void TryRemoveAssignment(Assignment a)
    {
        foreach (var group in Groups.Instance.groups)
        {
            if (group.assignments.Contains(a)) 
            {
                group.assignments.Remove(a);
                OnRemoveAssignment(a);
                return;
            }
        } throw new ArgumentException("Could not remove assignment " + a.Title + " from assignments as it is not in the list");
    }
    public static Assignment GetFocusedAssignment(int index) => FocusedAssignments[index];
    public static Assignment GetAssignment(Group group, int index) => group.assignments[index];
    public static void AddAssignment(Assignment a)
    {
        FocusedAssignments.Add(a);
    }
    private void Awake()
    {
        Instance = this;
        _assignments = new();

        _InitializeAddButton();

        /*if (PlayerPrefs.HasKey("LastLoadedFile"))
        {
            SaveManager.instance.LoadData();
        }*/
        // Main._InitializeWarnings();
    }
    public Transform PendingAssignments, CompletedAssignments;
    public GameObject baseAssignment, basePersistentAssignment, baseRandomAssignment, basePersistentReminder;
    public Button addButton;
    private void _InitializeAddButton()
    {
        addButton.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(IEnum_OnAddAssignment());
        });
    }
    private static readonly Dictionary<string, Type> _AddAssignmentValueRequests = new()
    {
        { "Title", typeof(string) },
        { "Start Date (optional)", typeof(DateTime?) },
        { "End Date", typeof(DateTime) }
    };
    ///<summary>
    /// The new assignment will be sent to Inputs.LastInput<Assignment>().
    ///</summary>
    public static Coroutine RequestAssignment() {
        return CommonGameManager.Coroutine(Instance.IEnum_OnAddAssignment());
    }
    private IEnumerator IEnum_OnAddAssignment(int md = 1, AssignmentType type = default)
    {
        switch (md)
        {
            case 1:
                type = default;
                #region Get Requested Assignment Type
                var typeScreen = Inputs.ScreensDictionary["Set Assignment Type"];
                Inputs.LoadScreen(typeScreen, scrn => {
                    type = (AssignmentType)typeScreen["Assignment Type"];
                },
                scrn =>
                {
                    return false;
                });

                yield return new WaitUntil(() => typeScreen.Unloaded);
                if (typeScreen.backed) {
                    break;
                }
                #endregion
                goto case 2;
            case 2:
                Assignment assignment = null;
                Screen screen = null;
                #region Set Assignment Class by Requested Type
                switch (type)
                {
                    case AssignmentType.Normal:
                    {
                        Screen subscreen = null;
                        screen = Inputs.ScreensDictionary["New Assignment"];
                        screen["Set Date Relatively"] = new Decision() {
                            action = () => {
                                subscreen = Inputs.ScreensDictionary["New Assignment (R)"];
                                Inputs.LoadScreen(subscreen, scrn => {
                                    var reminder = new NormalAssignment(subscreen["Title"].ToString(), DateTime.Now + (TimeSpan)subscreen["Start Time Delay"], DateTime.Now + (TimeSpan)subscreen["End Time Delay"], (string)subscreen["Note"]);
                                    AddAssignment(reminder);
                                },
                                scrn => {
                                    return false;
                                });
                            }
                        };
                        Inputs.LoadScreen(screen, 
                        scrn =>
                        {
                            assignment = new NormalAssignment(screen["Title"].ToString(), (DateTime?)screen["Start Date (optional)"], (DateTime)screen["End Date"], (string)screen["Note"]);
                            Debug.Log("Called add assignment on " + screen["Title"] + " once");
                            AddAssignment(assignment);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        while (!screen.Unloaded) {
                            yield return new WaitUntil(() => screen.Unloaded || (subscreen != null && subscreen.Unloaded));
                            if (subscreen != null && !subscreen.backed) {
                                while (Inputs.Screens.Count > 0) {
                                    Inputs.Screens.Pop();
                                }
                                Main.ShowMainScreen();
                                break;
                            }
                        }
                        break;
                    }
                    case AssignmentType.Persistent:
                    {
                        screen = Inputs.ScreensDictionary["New Persistent Assignment"];
                        Inputs.LoadScreen(screen, scrn =>
                        {
                            assignment = new PersistentAssignment(screen["Title"].ToString(), (TimeSpan)screen["Delay After Completion"], (DateTime)screen["Start Date"], (DateTime)screen["End Date"], (string)screen["Note"]);
                            AddAssignment(assignment);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        break;
                    }
                    case AssignmentType.Task:
                    {
                        screen = Inputs.ScreensDictionary["New Task"];
                        Inputs.LoadScreen(screen, scrn => {
                            assignment = new Task(screen["Title"].ToString(), (TimeData)screen["Start Time"], (TimeData)screen["End Time"], (string)screen["Note"]);
                            AddAssignment(assignment);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        break;
                    }
                    case AssignmentType.Random:
                    {
                        screen = Inputs.ScreensDictionary["New Random Assignment"];
                        Inputs.LoadScreen(screen, scrn =>
                        {
                            assignment = new NormalAssignment(screen["Title"].ToString(), (DateTime?)screen["Start Date (optional)"], (DateTime)screen["End Date"], (string)screen["Note"]);
                            assignment = new RandomAssignment(assignment, (TimeSpan)screen["Minimum Random Alert Delay"], (TimeSpan)screen["Maximum Random Alert Delay"], null);
                            AddAssignment(assignment);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        break;
                    }
                    case AssignmentType.Reminder:
                    {
                        screen = Inputs.ScreensDictionary["New Reminder"];

                        Screen subscreen = null;
                        screen["Set Date Relatively"] = new Decision() {
                            action = () => {
                                subscreen = Inputs.ScreensDictionary["New Reminder (R)"];
                                Inputs.LoadScreen(subscreen, scrn => {
                                    var reminder = new Reminder(subscreen["Title"].ToString(), DateTime.Now + (TimeSpan)subscreen["Delay"], (string)subscreen["Note"]);
                                    AddAssignment(reminder);
                                },
                                scrn => {
                                    return false;
                                });
                            }
                        };
                        Inputs.LoadScreen(screen, scrn =>
                        {
                            var reminder = new Reminder(screen["Title"].ToString(), (DateTime)screen["Date"], (string)screen["Note"]);
                            AddAssignment(reminder);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        while (!screen.Unloaded) {
                            yield return new WaitUntil(() => screen.Unloaded || (subscreen != null && subscreen.Unloaded));
                            if (subscreen != null && !subscreen.backed) {
                                while (Inputs.Screens.Count > 0) {
                                    Inputs.Screens.Pop();
                                }
                                Main.ShowMainScreen();
                                break;
                            }
                        }
                        break;
                    }
                    case AssignmentType.PersistentReminder:
                    {
                        screen = Inputs.ScreensDictionary["New Persistent Reminder"];
                        Inputs.LoadScreen(screen, scrn => 
                        {
                            var reminder = new PersistentReminder(screen["Title"].ToString(), (DateTime)screen["Date"], (TimeSpan)screen["Delay After Completion"], (string)screen["Note"]);
                            AddAssignment(reminder); 
                        },
                        scrn => 
                        {
                            return false;
                        });
                        Inputs.OnLoad += () => screen.GetInputManager<InputTSManager>("Delay After Completion").Fill(TimeSpan.FromDays(1));
                        break;
                    }
                    case AssignmentType.Note:
                    {
                        screen = Inputs.ScreensDictionary["New Note"];
                        Inputs.LoadScreen(screen, scrn =>
                        {
                            var note = new Note(screen["Title"].ToString(), 
                                                screen["Description"].ToString());
                            AddAssignment(note);
                        },
                        scrn =>
                        {
                            return false;
                        });
                        break;
                    }
                }

                yield return new WaitUntil(() => screen.Unloaded);
                if (screen.backed) {
                    goto case 1;
                }
                #endregion
                break;
        }
        Rebuild();
    }
    public static Type GetType(AssignmentType t) {
        return t switch {
            AssignmentType.Normal => typeof(NormalAssignment),
            AssignmentType.Persistent => typeof(PersistentAssignment),
            AssignmentType.Task => typeof(Task),
            AssignmentType.Random => typeof(RandomAssignment),
            AssignmentType.Reminder => typeof(Reminder),
            AssignmentType.PersistentReminder => typeof(PersistentReminder),
            AssignmentType.ContinuousRandom => typeof(ContinuousRandom),
            _ => typeof(object),
        };
    }
    public static void Rebuild()
    {
        DestroyAllAssignments();
        for (int i = 0; i < FocusedAssignments.Count; i++)
        {
            BuildAssignment(FocusedAssignments[i], i + 1);
        }
        OnAssignmentsRebuilt?.Invoke();
        Groups.RebuildGroups();
    }
    public static void DestroyAllAssignments()
    {
        foreach (Transform child in Instance.PendingAssignments)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in Instance.CompletedAssignments)
        {
            Destroy(child.gameObject);
        }
    }

    public static void BuildAssignment(Assignment a, int index)
    {
        switch (a)
        {
            case RandomAssignment ra:
                BuildRandomAssignment(ra, index);
                break;
            case PersistentAssignment pa:
                BuildPersistentAssignment(pa, index);
                break;
            case Task t:
                BuildTask(t, index);
                break;
            case PersistentReminder pr:
                BuildPersistentReminder(pr, index);
                break;
            case Reminder r:
                BuildReminder(r, index);
                break;
            case ContinuousRandom rr:
                BuildRandomReminder(rr, index);
                break;
            case Note n:
                BuildNote(n, index);
                break;
            default:
                BuildNormalAssignment(a, index);
                break;
        }
    }
    // Initialize an assignment as you usually would--set options as regular, set duedate and begindate texts as usual, etc.
    #region DefaultInitializeAssignment(Transform @base, Assignment a, int index)
    private static void DefaultInitializeAssignment(Transform @base, Assignment a, int index)
    {
        var title = @base.Find("Title").GetComponent<Text>();
        
        var beginDate = @base.Find("BeginDate").GetComponent<Text>();
        var dueDate = @base.Find("DueDate").GetComponent<Text>();

        title.text = a.Title;
        dueDate.text = "Due: " + a.DueDate.ToString("g",
                       CultureInfo.GetCultureInfo("en-US"));
        if (!a.HasStartDate)
        {
            a.SetStartDate(DateTime.Now);
        }
        beginDate.text = "Begin: " + a.StartDate.ToString("g",
                         CultureInfo.GetCultureInfo("en-US"));

        @base.name = nameof(Assignment) + index;

        var toggleDone = @base.Find("ToggleDone").GetComponent<Button>();
        const string MarkAsDoneText = "Mark as Done", MarkAsNotDoneText = "Restore";
        toggleDone.GetComponentInChildren<Text>().text = a.IsDone ? MarkAsNotDoneText : MarkAsDoneText;
        toggleDone.onClick.AddListener(() =>
        {
            a.ToggleDone();
        });

        var options = @base.Find("Options/Options/ScrollArea/VLG");
        Button changeDueDate = options.Find("ChangeDueDate").GetComponent<Button>(),
               changeStartDate = options.Find("ChangeStartDate").GetComponent<Button>(),
               changeTitle = options.Find("ChangeTitle").GetComponent<Button>(),
               setAssignmentType = options.Find("SetAssignmentType").GetComponent<Button>(),
               removeAssignment = options.Find("RemoveAssignment").GetComponent<Button>(),
               seeNote = options.Find("SeeNote").GetComponent<Button>();
        changeDueDate.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetDueDate(a));
        });
        if (changeStartDate) changeStartDate.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetStartDate(a));
        });
        changeTitle.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetTitle(a));
        });
        setAssignmentType.onClick.AddListener(() =>
        {
            TryRemoveAssignment(a);
            CommonGameManager.Coroutine(_SetAssignmentType(a));
        });
        removeAssignment.onClick.AddListener(() =>
        {
            TryRemoveAssignment(a);
            Rebuild();
        });
        seeNote.onClick.AddListener(() => {
            CommonGameManager.Coroutine(_SetNote(a));
        });
    }
    #endregion
    #region Other initialization methods
    private static void InitializePersistentReminder(Transform @base, PersistentReminder pr, int index)
    {
        var title = @base.Find("Title").GetComponent<Text>();
        
        var delay = @base.Find("Delay").GetComponent<Text>();
        var dueDate = @base.Find("DueDate").GetComponent<Text>();

        title.text = pr.Title;
        dueDate.text = "Due: " + pr.DueDate.ToString("g",
                       CultureInfo.GetCultureInfo("en-US"));
        delay.text = "Pause for: " + pr.Increment;
        @base.name = nameof(Assignment) + index;

        var toggleDone = @base.Find("ToggleDone").GetComponent<Button>();
        const string MarkAsDoneText = "Mark as Done", MarkAsNotDoneText = "Restore";
        toggleDone.GetComponentInChildren<Text>().text = pr.IsDone ? MarkAsNotDoneText : MarkAsDoneText;
        toggleDone.onClick.AddListener(() =>
        {
            pr.ToggleDone();
            if (pr.IsDone) {
                pr.MarkCompleted();
            } else {
                pr.UnmarkCompleted();
            }
        });

        var options = @base.Find("Options/Options/ScrollArea/VLG");
        Button changeDueDate = options.Find("ChangeDueDate").GetComponent<Button>(),
               changeTitle = options.Find("ChangeTitle").GetComponent<Button>(),
               changeDelay = options.FindComponent<Button>("ChangeDelay"),
               setAssignmentType = options.Find("SetAssignmentType").GetComponent<Button>(),
               removeAssignment = options.Find("RemoveAssignment").GetComponent<Button>();
        changeDueDate.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetDueDate(pr));
        });
        changeTitle.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetTitle(pr));
        });
        changeDelay.onClick.AddListener(() => {
            CommonGameManager.Coroutine(_SetDelay(pr));
        });
        setAssignmentType.onClick.AddListener(() =>
        {
            TryRemoveAssignment(pr);
            CommonGameManager.Coroutine(_SetAssignmentType(pr));
        });
        removeAssignment.onClick.AddListener(() =>
        {
            TryRemoveAssignment(pr);
            Rebuild();
        });
    }
    private static void InitializeRandomReminder(Transform @base, ContinuousRandom rr, int index)
    {
        var title = @base.Find("Title").GetComponent<Text>();
        
        var delay = @base.Find("Delay").GetComponent<Text>();
        var startDate = @base.Find("DueDate").GetComponent<Text>();

        title.text = rr.Title;
        startDate.text = "Start: " + rr.StartDate.ToString("g",
                       CultureInfo.GetCultureInfo("en-US"));
        delay.text = "Alert at: " + rr.NextAlarmDate.ToString("g", 
                     CultureInfo.GetCultureInfo("en-US"));
        @base.name = nameof(Assignment) + index;

        var toggleDone = @base.Find("ToggleDone").GetComponent<Button>();
        const string MarkAsDoneText = "Mark as Done", MarkAsNotDoneText = "Restore";
        toggleDone.GetComponentInChildren<Text>().text = rr.IsDone ? MarkAsNotDoneText : MarkAsDoneText;
        toggleDone.onClick.AddListener(rr.ToggleDone);

        var options = @base.Find("Options/Options/ScrollArea/VLG");
        Button changeDueDate = options.Find("ChangeDueDate").GetComponent<Button>(),
               changeTitle = options.Find("ChangeTitle").GetComponent<Button>(),
               changeDelay = options.FindComponent<Button>("ChangeDelay"),
               setAssignmentType = options.Find("SetAssignmentType").GetComponent<Button>(),
               removeAssignment = options.Find("RemoveAssignment").GetComponent<Button>();
        Destroy(changeDelay.gameObject);
        changeDueDate.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetDueDate(rr));
        });
        changeTitle.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetTitle(rr));
        });
        setAssignmentType.onClick.AddListener(() =>
        {
            TryRemoveAssignment(rr);
            CommonGameManager.Coroutine(_SetAssignmentType(rr));
        });
        removeAssignment.onClick.AddListener(() =>
        {
            TryRemoveAssignment(rr);
            Rebuild();
        });
    }
    private static void InitializePersistentAssignment(Transform @base, PersistentAssignment pa, int index)
    {
        var title = @base.Find("Title").GetComponent<Text>();

        var delay = @base.Find("Delay").GetComponent<Text>();
        var startDate = @base.Find("StartDate").GetComponent<Text>();
        var dueDate = @base.Find("DueDate").GetComponent<Text>();

        title.text = pa.Title;
        delay.text = "Pause for: " + pa.Increment;
        startDate.text = "Start: " + pa.StartDate.ToString("g",
                       CultureInfo.GetCultureInfo("en-US"));
        dueDate.text = "Due: " + pa.DueDate.ToString("g",
                       CultureInfo.GetCultureInfo("en-US"));
        @base.name = nameof(Assignment) + index;

        var toggleDone = @base.Find("ToggleDone").GetComponent<Button>();
        const string MarkAsDoneText = "Mark as Done", MarkAsNotDoneText = "Restore";
        toggleDone.GetComponentInChildren<Text>().text = pa.IsDone ? MarkAsNotDoneText : MarkAsDoneText;
        toggleDone.onClick.AddListener(() =>
        {
            pa.ToggleDone();
            if (pa.IsDone)
            {
                pa.MarkCompleted(DateTime.Now);
            }
            else
            {
                pa.UnmarkCompleted();
            }
        });

        var options = @base.Find("Options/Options/ScrollArea/VLG");
        Button changeStartDate = options.FindComponent<Button>("ChangeStartDate"),
               changeDueDate = options.Find("ChangeDueDate").GetComponent<Button>(),
               changeTitle = options.Find("ChangeTitle").GetComponent<Button>(),
               changeDelay = options.FindComponent<Button>("ChangeDelay"),
               setAssignmentType = options.Find("SetAssignmentType").GetComponent<Button>(),
               removeAssignment = options.Find("RemoveAssignment").GetComponent<Button>();
        changeStartDate.onClick.AddListener(() =>
        {
            print("sargon");
            CommonGameManager.Coroutine(_SetStartDate(pa));
        });
        changeDueDate.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetDueDate(pa));
        });
        changeTitle.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetTitle(pa));
        });
        changeDelay.onClick.AddListener(() => {
            CommonGameManager.Coroutine(_SetDelay(pa));
        });
        setAssignmentType.onClick.AddListener(() =>
        {
            TryRemoveAssignment(pa);
            CommonGameManager.Coroutine(_SetAssignmentType(pa));
        });
        removeAssignment.onClick.AddListener(() =>
        {
            TryRemoveAssignment(pa);
            Rebuild();
        });
    }
    private static void InitializeNote(Transform @base, Note n, int index)
    {
        var title = @base.Find("Title").GetComponent<Text>();
        
        var delay = @base.Find("Delay").GetComponent<Text>();
        var dueDate = @base.Find("DueDate").GetComponent<Text>();

        title.text = n.Title;  
        delay.text = n.Description;
        dueDate.text = null;
        @base.name = nameof(Assignment) + index;

        var toggleDone = @base.Find("ToggleDone").GetComponent<Button>();
        const string MarkAsDoneText = "Mark as Done", MarkAsNotDoneText = "Restore";
        toggleDone.GetComponentInChildren<Text>().text = n.IsDone ? MarkAsNotDoneText : MarkAsDoneText;
        toggleDone.onClick.AddListener(n.ToggleDone);

        var options = @base.Find("Options/Options/ScrollArea/VLG");
        Button changeDueDate = options.Find("ChangeDueDate").GetComponent<Button>(),
               changeTitle = options.Find("ChangeTitle").GetComponent<Button>(),
               setAssignmentType = options.Find("SetAssignmentType").GetComponent<Button>(),
               removeAssignment = options.Find("RemoveAssignment").GetComponent<Button>();
        Destroy(changeDueDate.gameObject);
        changeTitle.onClick.AddListener(() =>
        {
            CommonGameManager.Coroutine(_SetTitle(n));
        });
        setAssignmentType.onClick.AddListener(() =>
        {
            TryRemoveAssignment(n);
            CommonGameManager.Coroutine(_SetAssignmentType(n));
        });
        removeAssignment.onClick.AddListener(() =>
        {
            TryRemoveAssignment(n);
            Rebuild();
        });
    }
    #endregion
    #region Assignment Build Methods
    private static Transform BuildNormalAssignment(Assignment a, int index)
    {
        var @base = Instantiate(Instance.baseAssignment, a.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        DefaultInitializeAssignment(@base, a, index);
        return @base;
    }
    private static Transform BuildPersistentAssignment(PersistentAssignment a, int index)
    {
        var @base = Instantiate(Instance.basePersistentAssignment, a.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        InitializePersistentAssignment(@base, a, index);
        return @base;
    }
    private static Transform BuildRandomAssignment(RandomAssignment a, int index)
    {
        var @base = Instantiate(Instance.baseRandomAssignment, a.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        DefaultInitializeAssignment(@base, a, index);
        var nextAlert = @base.Find("NextAlert").GetComponent<Text>();
        nextAlert.text = "Next Alert: " + a.NextAlarmDate.ToString("g", CultureInfo.GetCultureInfo("en-US"));
        return @base;
    }
    private static Transform BuildTask(Assignment a, int index) {
        var @base = Instantiate(Instance.baseAssignment, a.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        DefaultInitializeAssignment(@base, a, index);
        return @base;
    }
    private static Transform BuildReminder(Reminder r, int index) {
        var @base = Instantiate(Instance.baseAssignment, r.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        DefaultInitializeAssignment(@base, r, index);
        Text beginDate = @base.FindComponent<Text>("BeginDate"), 
             dueDate = @base.FindComponent<Text>("DueDate");
        dueDate.text = "Date: " + r.DueDate.ToString("g",
               CultureInfo.GetCultureInfo("en-US"));
        beginDate.text = "";
        return @base;
    }
    private static Transform BuildPersistentReminder(PersistentReminder pr, int index) {
        var @base = Instantiate(Instance.basePersistentReminder, pr.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        InitializePersistentReminder(@base, pr, index);
        return @base;
    }
    private static Transform BuildRandomReminder(ContinuousRandom rr, int index) {
        var @base = Instantiate(Instance.basePersistentReminder, rr.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        InitializeRandomReminder(@base, rr, index);
        return @base;
    }
    private static Transform BuildNote(Note n, int index) {
        var @base = Instantiate(Instance.basePersistentReminder, n.IsDone ? Instance.CompletedAssignments : Instance.PendingAssignments).transform;
        InitializeNote(@base, n, index);
        return @base;
    }
    #endregion
    #region Assignment-Specific Data Update Coroutines...
    private static IEnumerator _SetDueDate(Assignment a)
    {
        // yield return Main.Instance.StartCoroutine(Inputs.AwaitValues("New Assignment Due Date", typeof(DateTime)));
        // a.SetDueDate(Inputs.GetLastInput<DateTime>());

        var screen = new Screen("New Assignment Due Date", new() { { "DateTime", typeof(DateTime) } });
        screen["DateTime"] = a.DueDate;
        Inputs.LoadScreen(screen, scrn => {
            a.SetDueDate((DateTime)screen["DateTime"]);
        },
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        // New Version
        /*
         * var screen = Inputs.ScreensDictionary["New Assignment Due Date"];
         * Inputs.LoadScreen(screen, scrn => { 
         *  if (scrn == screen)
         *      a.SetDueDate(Inputs.GetLastInput<DateTime>());
         *  return scrn == screen;
         * });
         * yield return new WaitUntil(() => screen.Unloaded);
         */
    }
    private static IEnumerator _SetStartDate(Assignment a)
    {
        print("started set start date");
        var screen = new Screen("New Assignment Start Date", new() { { "DateTime", typeof(DateTime?) } });
        screen["DateTime"] = a.HasStartDate ? a.StartDate : null;
        Inputs.LoadScreen(screen, scrn => {
            a.SetStartDate((DateTime?)screen["DateTime"]);
        },
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);
    }
    private static IEnumerator _SetTitle(Assignment a)
    {
        var screen = new Screen("New Assignment Title", new() { { "String", typeof(string) } });
        screen["String"] = a.Title;
        Inputs.LoadScreen(screen, scrn => {
            a.SetTitle(screen["String"].ToString());
        },
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);
    }

    private static IEnumerator _SetDelay(PersistentReminder pr) {
        var screen = new Screen("Set Delay",
        new() {
            { "Delay", typeof(TimeSpan) }
        });
        Inputs.LoadScreen(screen, scrn => pr.Increment = (TimeSpan)scrn["Delay"], null);
        yield return new WaitUntil(() => screen.Unloaded);
        Assignments.Rebuild();
    }
    private static IEnumerator _SetDelay(PersistentAssignment pa)
    {
        var screen = new Screen("Set Delay",
        new() {
            { "Delay", typeof(TimeSpan) }
        });
        Inputs.LoadScreen(screen, scrn => pa.Increment = (TimeSpan)scrn["Delay"], null);
        yield return new WaitUntil(() => screen.Unloaded);
        Assignments.Rebuild();
    }
    private static readonly Dictionary<InputDescription, Type> _AddRandomAssignmentRequests = new()
    {
        { "Minimum Random Alert Delay", typeof(TimeSpan) },
        { "Maximum Random Alert Delay", typeof(TimeSpan) },
    };
    private static IEnumerator _SetAssignmentType(Assignment a)
    {
        var screen = Inputs.ScreensDictionary["Set Assignment Type"];
        AssignmentType type = default;
        Inputs.LoadScreen(screen, scrn => {
            type = (AssignmentType)screen["Assignment Type"];
        },
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        switch (type)
        {
            case AssignmentType.Random:
                screen = new Screen("Set Alert Delay Range", _AddRandomAssignmentRequests);
                Inputs.LoadScreen(screen, scrn => {
                    var timeSpans = Inputs.GetInputs<TimeSpan>();
                    var randomA = new RandomAssignment(a, timeSpans[0], timeSpans[1], a.note);

                    AddAssignment(randomA);
                },
                scrn =>
                {
                    return false;
                });
                yield return new WaitUntil(() => screen.Unloaded);
                break;
            /*
            // TODO: allow for setting the increment when an assignment is changed to be a persistent assignment
            case AssignmentType.Persistent:
                PersistentAssignment pa = new PersistentAssignment(a.Title, a.StartDate, a.DueDate);
                Instance._assignments.Add(pa);
                break;
            */
            case AssignmentType.Task:
                Task t = new Task(a.Title, a.StartDate, a.DueDate, a.note);
                AddAssignment(t);
                break;
            default:
                AddAssignment(a);
                break;
        }
        Rebuild();

        /* New Version
         * var screen = Inputs.ScreensDictionary["Set Assignment Type"];
         * AssignmentType type = default;
         * Inputs.LoadScreen(screen, scrn => {
         *  if (scrn == screen) {
         *      type = Inputs.GetLastInput<AssignmentType>();
         *  });
         *  yield return new WaitUntil(() => screen.Unloaded);
         *  
         *  switch (type)
            {
                case AssignmentType.Random:
                    screen = new Screen("Set Alert Delay Range", _AddRandomAssignmentRequests);
                    Inputs.LoadScreen(screen, scrn => {

                        var timeSpans = Inputs.GetInputs<TimeSpan>();
                        var randomA = new RandomAssignment(a, timeSpans[0], timeSpans[1]);

                        Instance._assignments.Add(randomA);
                    });
                    yield return new WaitUntil(() => screen.Unloaded);
                    break;
                default:
                    Instance._assignments.Add(a);
                    break;
            }
         *  Rebuild();
         */

        /*
        yield return Main.Instance.StartCoroutine(Inputs.AwaitValues("Set Assignment Type", typeof(AssignmentType)));
        var type = Inputs.GetLastInput<AssignmentType>();

        switch (type)
        {
            case AssignmentType.Random:
                yield return Main.Instance.StartCoroutine(Inputs.AwaitValues("Set Alert Delay Range", _AddRandomAssignmentRequests));

                var timeSpans = Inputs.GetInputs<TimeSpan>();
                var randomA = new RandomAssignment(a, timeSpans[0], timeSpans[1]);

                Instance._assignments.Add(randomA);
                break;
            default:
                Instance._assignments.Add(a);
                break;
        }
        */
    }
    private static IEnumerator _SetNote(Assignment assignment) 
    {
        var screen = Inputs.ScreensDictionary["See Note"];
        Inputs.LoadScreen(screen, scrn => {
            assignment.note = screen["Note"] as string;
        }, scrn => false);
        Inputs.OnLoad += () => screen.GetInputManager<InputSManager>("Note").Fill(assignment.note);
        yield return new WaitUntil(() => screen.Unloaded);
    }
    #endregion
}
