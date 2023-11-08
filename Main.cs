using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using Common;
using Common.Helpers;
using Common.Yields;
using Common.Extensions;

using URandom = UnityEngine.Random;

using Text = TMPro.TextMeshProUGUI;
public class RandomAlert
{
    private Assignment _ref;
    private TimeSpan _currentDelay;
    private TimeSpan _min, _max;
    public RandomAlert(Assignment @ref, TimeSpan min, TimeSpan max)
    {
        _ref = @ref;
        _currentDelay = default;
        _min = min;
        _max = max;
    }
    public void SetCurrentDelay() => _currentDelay = URandom.value * (_max - _min) + _min;
    public void SetMinPause(TimeSpan _newMin) => _min = _newMin;
    public void SetMaxPause(TimeSpan _newMax) => _min = _newMax;
}
public class Main : MonoBehaviour
{
    public static Transform mainScreen;
    public static void SetMainScreenAndShow(Transform screen) {
        mainScreen = screen;
        ShowMainScreen();
    }
    public static void ShowMainScreen() {
        mainScreen.IsolateSelf();
    }
    public static void DisableUpdate(bool @throw = false)
    {
        if (!@throw && !Instance)
            return;
        Instance.enabled = false;
    }
    public static void EnableUpdate(bool @throw = false)
    {
        if (!@throw && !Instance)
            return;
        Instance.enabled = true;
    }

    public static List<RandomAlert> _alerts;
    public static Main Instance;
    private void Awake()
    {
        Assignments.OnRemoveAssignment += (a) => { 
            RecheckDueDates();
            Assignments.Rebuild();
        };
        Application.runInBackground = true;
        TryGetComponent(out sfx);
        _timer = new(0f, CheckDueDatesEvery);
        Instance = this;

        Assignments.OnRemoveAssignment += a => _dueDateWarningTimes.Remove(a);
        print(ComponentHelper.FindObjectOfName<MySizeFitter>("ListPending"));
        Assignments.OnAssignmentsRebuilt += ComponentHelper.FindObjectOfName<MySizeFitter>("ListPending").FitToChildren;
        // Assignments.OnAssignmentsRebuilt += _CheckAllDueDates;

        _RandomAlertToggle.onValueChanged.AddListener(isOn =>
        {
            _alerting = isOn;
            if (isOn)
            {
                ToggleRandomAlerts();
            }
        });

        mainScreen = GroupMain;
    }
    private void Start()
    {
        bool allActive = true;
        foreach (Transform t in transform)
        {
            if (!t.gameObject.activeInHierarchy)
            {
                allActive = false;
            }
        }
        if (!allActive)
        {
            Debug.LogError("You may have forgotten to enable all screens before playing.", this);
        }
        // _InitializeWarnings();
        _exitGroupText = GameObject.Find("ExitGroup").GetComponentInChildren<Text>();
    }

    private bool _alerting;

    private Timer _saveTimer = new(0f, SaveTimerTime);
    private const int SaveTimerTime = 3; // Save assignments every n due date checks

    static bool focused = false;
    public static bool FocusedOnGroup => focused;
    public Transform GroupMain, AssignmentMain, GroupConditionsMain;
    public static void InitializeFocusedGroup(Group g)
    {
        focused = true;
        _InitializeWarnings();
        SetMainScreenAndShow(Instance.AssignmentMain);
        Assignments.Instance.addButton.interactable = g != Groups.activeGroup;

        Instance._exitGroupText.text = "Exit Group (" + Groups.focusedGroup.name + ")";
        Assignments.Rebuild();
    }
    public static Coroutine RequestAddConditions(Group group = null, string name = null) {
        IEnumerator IEnum() {
            if (name != null) {
                LoadGroupCondition.SetGroupName(name);
            } else {
                LoadGroupCondition.SetTargetGroup(group);
            }
            print("WE HIT IT!");
            SetMainScreenAndShow(Instance.GroupConditionsMain);
            yield return new WaitUntil(() => !Instance.GroupConditionsMain.gameObject.activeSelf); // will disable itself
        }
        return CommonGameManager.Coroutine(IEnum());
    }

    private Text _exitGroupText;
    public static void UnfocusGroup()
    {
        focused = false;
        Groups.focusedGroup = null;
        Instance.GroupMain.IsolateSelf();
        StopCheckingDueDates();
        Groups.RebuildGroups();
    }
    private void Update()
    {
        if (focused)
        {
            if (SaveManager.instance.autosave && _saveTimer.ClampedIncrementHit(true, true, true))
            {
                SaveManager.instance.SaveData();
            }
            if (!isChecking && _timer.ClampedIncrementHit(true, true, false))
            {
                _CheckAllDueDates();
            }
            if (_alerting && Assignments.AnyAssignments() && !alerted && DelaysSet())
            {
                _TriggerRandomAlert();
            }
            print("IS_ALERTING: " + _alerting + "\n" + 
                  "ANY ASSIGNMENTS: " + Assignments.AnyAssignments() + "\n" +
                  "NOT ALERTED: " + !alerted + "\n" +
                  "DELAYS ARE SET: " + DelaysSet());
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopAnnouncing();
            }
        }
        /*if (_timer.ClampedIncrementHit(true, true, false))
        {
            Groups.RebuildGroups();
            _timer.SetZero();
        }*/
    }

    public void StopAnnouncing()
    {
        Instance.sfx.PlaySFXIndirect(""); // Shut up
        _announce = false;
    }
    SFXData sfx;
    // private List<float> _startDateWarningTimes; // -1 == never warned
    private Dictionary<Assignment, float> _dueDateWarningTimes; // -1 == never warned
    public float CheckDueDatesEvery = 30f; // 30 seconds

    public Toggle _RandomAlertToggle;
    public DateTime _lastAlertTime;
    public TimeSpan _RandomAlertMinDelay, _RandomAlertMaxDelay, _RandomAlertNextDelay;
    private bool DelaysSet() => _RandomAlertMinDelay != TimeSpan.Zero || _RandomAlertMaxDelay != TimeSpan.Zero;
    private void _SetNextRandomAlertDelay()
    {
        _lastAlertTime = DateTime.Now;
        _RandomAlertNextDelay = new TimeSpan(_RandomAlertMinDelay.Ticks + (long)(URandom.value * (_RandomAlertMaxDelay.Ticks - _RandomAlertMinDelay.Ticks)));
    }
    private void ToggleRandomAlerts()
    {
        StartCoroutine(IEnum_ToggleRandomAlerts());
    }
    private IEnumerator IEnum_ToggleRandomAlerts()
    {
        Screen screen = Inputs.ScreensDictionary["Random Alert Frequency"];
        Inputs.LoadScreen(screen, scrn => {          
            _RandomAlertMinDelay = (TimeSpan)screen["Minimum Delay"];
            _RandomAlertMaxDelay = (TimeSpan)screen["Maximum Delay"];
            _SetNextRandomAlertDelay();
            _lastAlertTime = DateTime.Now;
        }, scrn => false);
        yield return new WaitUntil(() => screen.Unloaded);
    }

    private Timer _timer;
    // indices align with assignments
    public static void _InitializeWarnings()
    {
        Instance._dueDateWarningTimes = new Dictionary<Assignment, float>();
        for (int i = 0; i < Assignments.FocusedAssignments.Count; i++)
        {
            Instance._dueDateWarningTimes[Assignments.FocusedAssignments[i]] = -1;
        }
    }

    bool alerted;
    private void _TriggerRandomAlert()
    {
        print(_RandomAlertNextDelay.TotalSeconds);
        print(_lastAlertTime + _RandomAlertNextDelay);
        if (DateTime.Now >= _lastAlertTime + _RandomAlertNextDelay)
        {
            alerted = true;
            _RandomAlert();
        }
    }
    private void _RandomAlert()
    {
        StartCoroutine(IEnum_RandomAlert());
    }
    private IEnumerator IEnum_RandomAlert()
    {
        var index = URandom.Range(0, Assignments.FocusedAssignments.Count);
        yield return Announce(index, "RandomAlert");
        _SetNextRandomAlertDelay();
        alerted = false;
    }
    private void _CheckAllDueDates()
    {
        _announce = true;
        if (checker != null) {
            return;
        }
        checker = StartCoroutine(IEnum_CheckAllDueDates());
    }

    public static bool _announce; // whether to put a delay to speak to the user after every alert related to an assignment
    public static Coroutine Announce(int assignmentIndex, string sound, bool waitUntilMarkedAsDone=false, Func<int, bool> muteCondition = null)
    {
        if (_announce)
            return _lastAnnouncement = Instance.StartCoroutine(IEnum_Announce(assignmentIndex, sound, muteCondition ?? ((i) => false), waitUntilMarkedAsDone));
        return null;
    }

    private static Coroutine _lastAnnouncement;
    private static IEnumerator IEnum_Announce(int i, string s, Func<int, bool> muteCondition, bool waitUntilMarkedAsDone=false)
    {
        Coroutine StartHighlightingAssignment() {
            IEnumerator _IEnum() {
                var a = Assignments.GetFocusedAssignment(i);
                while (true)
                {
                    Assignments.HighlightAssignment(i);
                    yield return null;
                }
            }
            return Instance.StartCoroutine(_IEnum());
        }

        Assignment assignment = Assignments.GetFocusedAssignment(i);
        bool canBreak = !waitUntilMarkedAsDone; // && !Assignments.GetFocusedAssignment(i).IsDone;
        do
        {
            yield return new WaitForEndOfFrame();
            var hlight = StartHighlightingAssignment();
            yield return Instance.sfx.PlaySFXIndirect(s);
            Instance.StopCoroutine(hlight);
            Assignments.UnhighlightAssignment(i);
            if (assignment.IsDone || muteCondition(i))
            {
                break;
            }
            if (waitUntilMarkedAsDone)
            {
                yield return new WaitForSecondsOrUntil(2f, () => canBreak = assignment.IsDone);
            }
        }
        while (!canBreak);
    }


    private bool isChecking;
    private Coroutine checker;
    public static void RecheckDueDates()
    {
        print("rechecking");
        StopCheckingDueDates();
        Instance._timer.SetMax();
        Assignments.Rebuild();
    }
    public static void StopCheckingDueDates()
    {
        Instance.sfx.PlaySFXIndirect(""); // shut the fuck up
        Instance.StopAllCoroutines();
        Instance.isChecking = false;
        Instance.checker = null;
    }
    private IEnumerator IEnum_CheckAllDueDates()
    {
        isChecking = true;
        var currentTime = DateTime.Now;
        for (int i = 0; i < Assignments.FocusedAssignments.Count; i++)
        {
            var assignment = Assignments.FocusedAssignments[i];
            if (!_dueDateWarningTimes.ContainsKey(assignment))
            {
                _dueDateWarningTimes[assignment] = -1;
            }

            if (assignment.IsDone && !assignment.RunWhenDone)
            {
                continue;
            }
            else
            {
                Coroutine update = null;
                Action<Assignment> onRemove = null;
                Assignments.OnRemoveAssignment += onRemove = a =>
                {
                    StopCoroutine(update);
                    Assignments.OnRemoveAssignment -= onRemove;
                };
                switch (assignment)
                {
                    case PersistentAssignment:
                        update = StartCoroutine(_PersistentAssignmentUpdate(currentTime, i));
                        break;
                    case RandomAssignment:
                        update = StartCoroutine(_RandomAssignmentUpdate(currentTime, i));
                        break;
                    case Task:
                        update = StartCoroutine(_TaskAssignmentUpdate(currentTime, i));
                        break;
                    case PersistentReminder:
                        update = StartCoroutine(_PersistentReminderAssignmentUpdate(currentTime, i));
                        break;
                    case Reminder:
                        update = StartCoroutine(_ReminderAssignmentUpdate(currentTime, i));
                        break;
                    case Note:
                        update = null;
                        break;
                    default:
                        update = StartCoroutine(_BaseAssignmentUpdate(currentTime, i));
                        break;
                }
                yield return update;
            }
        }
        isChecking = false;
        checker = null;
    }
    private IEnumerator _BaseAssignmentUpdate(DateTime currentTime, int i)
    {
        var assignment = Assignments.GetFocusedAssignment(i);
        if (currentTime > assignment.DueDate)
        {
            if (_dueDateWarningTimes[assignment] != 0)
            {
                Assignments.PushToTop(i);
                if (_announce)
                {
                    yield return Announce(0, "0HourWarning", true, (i) => assignment.DueDate > currentTime);
                }
                _dueDateWarningTimes[assignment] = 0;
            }
        }
        else if (assignment.HasStartDate && currentTime < assignment.StartDate)
        {
            yield return IEnum_CheckStartDate(currentTime, i);
        }
        else
        {
            var span = (assignment.DueDate - currentTime);
            var hourDifference = Mathf.CeilToInt((float)span.TotalHours);

            if (hourDifference == 0)
            {
                var hourFractionDifference = span.Minutes / 60f;
                switch (hourFractionDifference)
                {
                    case < 0.5f:
                        if (MathHelper.Between(_dueDateWarningTimes[assignment], 0, 0.5f))
                        {
                            break;
                        }
                        _dueDateWarningTimes[assignment] = hourFractionDifference;
                        break;
                }
            }
            else if (_dueDateWarningTimes[assignment] != hourDifference)
            {
                _dueDateWarningTimes[assignment] = hourDifference;
                switch (hourDifference)
                {
                    case 6:
                    case 4:
                    case 3:
                    case 1:
                        Assignments.PushToTop(i);
                        if (_announce)
                        {
                            yield return Announce(0, hourDifference + "HourWarning");
                        }
                        break;
                }
            }
        }
    }
    private IEnumerator _PersistentAssignmentUpdate(DateTime currentTime, int i)
    {
        var preminder = Assignments.GetFocusedAssignment(i) as PersistentAssignment;
        if (currentTime > preminder.DueDate)
        {
            Assignments.PushToTop(i);
            if (_announce)
            {
                yield return Announce(0, "PersistentAssignmentDueDatePassed", true, (i) => preminder.DueDate > currentTime);
                yield return new WaitForSecondsAnd(2f, () => Assignments.HighlightAssignment(0));
            }
            preminder.MarkCompleted(currentTime);
            preminder.SetDone(true);
        }
        else
        {
            yield return IEnum_CheckStartDate(currentTime, i);
        }
        if (MathHelper.Between(currentTime.Ticks, preminder.StartDate.Ticks, preminder.DueDate.Ticks))
        {
            preminder.SetDone(false);
        }
    }
    // We add a task at 10AM.
    // The task is to be performed between 6AM and 9AM.
    // The system detects it is currently past 9AM immediately after being added and yells at us.

    // We need to prevent this from happening immediately after the task is added.
    
    // If the task has just been added and it is past the duedate, do not alarm.
    // Essentially, we need to check that the current time has passed the duedate more than once.

    // If the due date of the task was set before the current time, set the necessary number of times completed 
    // before the warning is broadcasted to 2.

    // A task is considered completed if the below criteria is met:
    // 1. The current time is past the due time.
    // 2. The current date is past the date of last completion.

    // What we could do is set the date of last completion to the following day, instead.
    private IEnumerator _TaskAssignmentUpdate(DateTime currentDate, int index) {
        var task = Assignments.GetFocusedAssignment(index) as Task;
        if (currentDate > task.DueDate) {
            task.MarkCompleted();
        } else if (currentDate > task.StartDate) {
            task.SetDone(false);
        } else {
            task.SetDone(true);
        }
        // otherwise if current date is before the task's due date and they share year/month/day
        // else if (currentDate.YMD() == DateTime.Now.YMD()) {
        //    task.SetDone(false);
        // }
        if (!task.IsDone) {
            // print("IS " + task.Title + " DONE?: " + task.IsDone);
            yield return StartCoroutine(_BaseAssignmentUpdate(currentDate, index));
        }
    }
    private IEnumerator _ReminderAssignmentUpdate(DateTime currentTime, int i) 
    {
        var reminder = Assignments.GetFocusedAssignment(i) as Reminder;
        if (currentTime > reminder.DueDate)
        {
            Assignments.PushToTop(i); 
            if (_announce)
            {
                yield return Announce(0, "ReminderDatePassed", true, (i) => reminder.DueDate > currentTime);
                yield return new WaitUntilAnd(() => Application.isFocused, () => Assignments.HighlightAssignment(0));
                yield return new WaitForSecondsAnd(2f, () => Assignments.HighlightAssignment(0));
                if (currentTime < reminder.DueDate) // could've changed
                {
                    yield break;
                }
            }
            reminder.SetDone(true);
        }
        else {
            reminder.SetDone(false);
        }
    }
    private IEnumerator _PersistentReminderAssignmentUpdate(DateTime currentTime, int i) 
    {
        var preminder = Assignments.GetFocusedAssignment(i) as PersistentReminder;
        if (currentTime > preminder.DueDate)
        {
            Assignments.PushToTop(i);
            if (_announce)
            {
                yield return Announce(0, "ReminderDatePassed", true, (i) => preminder.DueDate > currentTime);
                yield return new WaitForSecondsAnd(2f, () => Assignments.HighlightAssignment(0));
            }
            preminder.MarkCompleted();
            preminder.SetDone(true);
        }
        if (currentTime <= preminder.DueDate && preminder.DueDate.YMD() == DateTime.Now.YMD()) { // same day?
            preminder.SetDone(false);
        }
    }
    private IEnumerator _RandomAssignmentUpdate(DateTime currentTime, int i)
    {
        var assignment = Assignments.GetFocusedAssignment(i) as RandomAssignment;
        if (currentTime > assignment.DueDate)
        {
            assignment.ToggleDone();

            Assignments.PushToTop(i);
            if (_announce)
            {
                yield return Announce(0, "RandomAssignmentMarkedAsDone", true);
            }
        }
        else if (assignment.HasStartDate)
        {
            yield return IEnum_CheckStartDate(currentTime, i);
        }
        else
        {
            if (assignment.IsPastAlarmDate)
            {
                Assignments.PushToTop(i);
                if (_announce)
                {
                    yield return Announce(0, "RandomAlertAssignmentReminder");
                }
                assignment.SetNextAlarmDate();
            }
        }
    }
    private IEnumerator IEnum_CheckStartDate(DateTime currentTime, int index)
    {
        var a = Assignments.GetFocusedAssignment(index);
        if (currentTime >= a.StartDate)
        {
            if (_dueDateWarningTimes[a] != 0)
            {
                Assignments.PushToTop(index);
                if (_announce)
                {
                    yield return Announce(0, "0HourWarningBegin");
                }
                _dueDateWarningTimes[a] = 0;
            }
        }
        else // currentTime < a.startDate, a.startDate > currentTime
        {
            TimeSpan timeDifference = a.StartDate - currentTime;
            int hourDifference = timeDifference.Hours + 1;
            if (hourDifference == 1 && _dueDateWarningTimes[a] != 1) // as an int it may be 1 if total minutes is below 60
            {
                var totalHours = timeDifference.TotalHours; // need to get hour fraction
                switch (totalHours)
                {
                    case < 0.5f: // if hour fraction < 0.5f (<30m left)
                        if (MathHelper.Between(_dueDateWarningTimes[a], 0, 0.5f)) // ensure we are not already within this time fraction
                        {
                            break;
                        }
                        Assignments.PushToTop(index);
                        if (_announce)
                        {
                            yield return Announce(0, "0.5HourWarningBegin"); // warning begin
                        }
                        _dueDateWarningTimes[a] = 0.5f; // set new hour warning 
                        break;
                }
            }
            else
            {
                if (_dueDateWarningTimes[a] != hourDifference)
                {
                    _dueDateWarningTimes[a] = hourDifference;
                    switch (hourDifference)
                    {
                        case 1:
                            Assignments.PushToTop(index);
                            if (_announce)
                            {
                                yield return Announce(0, hourDifference + "HourWarningBegin");
                            }
                            break;
                    }
                }
            }
        }
    }
}
