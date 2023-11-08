using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Text = TMPro.TextMeshProUGUI;
public class Actions : MonoBehaviour
{
    private static Actions instance;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        InitializeAllButtons();
    }

    // TODO:
    // Generate/Show Agenda
        // Goes through every assignment/reminder created and compiles an agenda over a given period of time based on types of assignments.
        // I.E. reminders are just notes with times attached, assignments are non-reoccuring time ranges, persistent assignments are reoccuring time ranges
    private void InitializeAllButtons()
    {
        var buttons = GetComponentsInChildren<Button>().ToList();
        Button delayAll = buttons.Find(b => b.name == "DelayAll"),
               delayOne = buttons.Find(b => b.name == "Delay"),
               pivotMode = buttons.Find(b => b.name == "PivotMode"),
               delayFuture = buttons.Find(b => b.name == "DelayFuture"),
               delayBefore = buttons.Find(b => b.name == "DelayBefore"),
               delayAfter = buttons.Find(b => b.name == "DelayAfter"),
               swapDates = buttons.Find(b => b.name == "SwapDates");
        delayAll.onClick.AddListener(_DelayAll);
        delayOne.onClick.AddListener(_DelayOne);
        //pivotMode.onClick.AddListener(_PivotMode);
        delayFuture.onClick.AddListener(_DelayFuture);
        delayBefore.onClick.AddListener(_DelayBefore);
        delayAfter.onClick.AddListener(_DelayAfter);
        swapDates.onClick.AddListener(_SwapDates);
    }
    #region Button On Clicks
    #region DelayAll
    private void _DelayAll()
    {
        Main.Instance.StartCoroutine(IEnum_DelayAll());
    }

    private static readonly Screen _DelayAllAssignments = new("Delay All Assignments", new() {
        { "TimeSpan", typeof(TimeSpan) },
        { new("Assignment Type", true), typeof(AssignmentType) }
    });
    private IEnumerator IEnum_DelayAll()
    {
        AssignmentType type = AssignmentType.Count;
        var screen = _DelayAllAssignments;
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return false;
        });
        
        yield return new WaitUntil(() => screen.Unloaded);

        var deltaTime = (TimeSpan)screen["TimeSpan"];
        for (int g = 0; g < Groups.GroupCount; g++) 
        {
            var group = Groups.GetGroup(g);
            for (int a = 0; a < group.assignments.Count; a++)
            {
                var assignment = Assignments.GetAssignment(group, a);
                var assignmentType = Assignments.GetType(type);
                if (a.GetType() != assignmentType && type != AssignmentType.Count) {
                    continue;
                }
                if (!assignment.IsDone || assignment.RunWhenDone)
                {
                    assignment.SetDueDate(assignment.DueDate + deltaTime);
                    assignment.SetStartDate(assignment.StartDate + deltaTime);
                }
            }
        }
    }
    #endregion
    #region DelayAfter
    private void _DelayAfter()
    {
        Main.Instance.StartCoroutine(IEnum_DelayAfter());
    }
    private IEnumerator IEnum_DelayAfter()
    {
        var screen = new Screen("Delay All Assignments After Date", new() { { "TimeSpan", typeof(TimeSpan) }, { "Date/Time", typeof(DateTime) } });
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        var deltaTime = (TimeSpan)screen["TimeSpan"];
        var minTime = (DateTime)screen["Date/Time"];
        for (int g = 0; g < Groups.GroupCount; g++) 
        {
            var group = Groups.GetGroup(g);
            for (int a = 0; a < group.assignments.Count; a++)
            {
                var assignment = Assignments.GetAssignment(group, a);
                if (!assignment.IsDone || assignment.RunWhenDone)
                {
                    if (assignment.DueDate > DateTime.Now && assignment.DueDate > minTime)
                    {
                        print("postponed " + assignment.Title + " by " + deltaTime);
                        assignment.SetDueDate(assignment.DueDate + deltaTime);
                    }
                    if (assignment.HasStartDate && assignment.StartDate > DateTime.Now && assignment.StartDate > minTime) 
                    {
                        assignment.SetStartDate(assignment.StartDate + deltaTime); 
                    }
                }
            }
        }
    }
    #endregion
    #region DelayBefore
    private void _DelayBefore()
    {
        Main.Instance.StartCoroutine(IEnum_DelayBefore());
    }
    private IEnumerator IEnum_DelayBefore()
    {
        var screen = new Screen("Delay All Assignments Before Date", new() { { "TimeSpan", typeof(TimeSpan) }, { "Date/Time", typeof(DateTime) } });
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        var deltaTime = (TimeSpan)screen["TimeSpan"];
        var maxTime = (DateTime)screen["Date/Time"];
        for (int g = 0; g < Groups.GroupCount; g++) 
        {
            var group = Groups.GetGroup(g);
            for (int a = 0; a < group.assignments.Count; a++)
            {
                var assignment = Assignments.GetAssignment(group, a);
                if (!assignment.IsDone || assignment.RunWhenDone)
                {
                    if (assignment.DueDate > DateTime.Now && assignment.DueDate < maxTime)
                    {
                        print("postponed " + assignment.Title + " by " + deltaTime);
                        assignment.SetDueDate(assignment.DueDate + deltaTime);
                    }
                    if (assignment.HasStartDate && assignment.StartDate > DateTime.Now && assignment.StartDate < maxTime) 
                    {
                        assignment.SetStartDate(assignment.StartDate + deltaTime); 
                    }
                }
            }
        }
    }
    #endregion
    #region DelayFuture
    private void _DelayFuture()
    {
        Main.Instance.StartCoroutine(IEnum_DelayFuture());
    }
    private IEnumerator IEnum_DelayFuture()
    {
        var screen = new Screen("Delay All Future Assignments", new() { { "TimeSpan", typeof(TimeSpan) } });
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        var deltaTime = (TimeSpan)screen["TimeSpan"];
        for (int g = 0; g < Groups.GroupCount; g++) 
        {
            var group = Groups.GetGroup(g);
            for (int a = 0; a < group.assignments.Count; a++)
            {
                var assignment = Assignments.GetAssignment(group, a);
                if (!assignment.IsDone || assignment.RunWhenDone)
                {
                    if (assignment.DueDate > DateTime.Now)
                    {
                        print("postponed " + assignment.Title + " by " + deltaTime);
                        assignment.SetDueDate(assignment.DueDate + deltaTime);
                    }
                    if (assignment.HasStartDate && assignment.StartDate > DateTime.Now) 
                    {
                        assignment.SetStartDate(assignment.StartDate + deltaTime); 
                    }
                }
            }
        }
    }
    #endregion
    #region Delay
    private void _DelayOne()
    {
        Main.Instance.StartCoroutine(IEnum_DelayOne());
    }
    private IEnumerator IEnum_DelayOne()
    {
        var screen = new Screen("Delay Assignment", new() { { "Assignment", typeof(Assignment) }, { "TimeSpan", typeof(TimeSpan) } });
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return true;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        var deltaTime = (TimeSpan)screen["TimeSpan"];
        var assignment = (Assignment)screen["Assignment"];
        
        if (!screen.backed && !assignment.IsDone || assignment.RunWhenDone)
        {
            assignment.SetDueDate(assignment.DueDate + deltaTime);
            if (assignment.HasStartDate)
            {
                assignment.SetStartDate(assignment.StartDate + deltaTime); 
            }
        }
    }
    #endregion
    #region PivotMode
    private void _PivotMode()
    {
        Main.Instance.StartCoroutine(IEnum_PivotMode());
    }
    private IEnumerator IEnum_PivotMode()
    {
        yield return null;
    }
    #endregion
    #region SwapDates
    private void _SwapDates()
    {
        Main.Instance.StartCoroutine(IEnum_SwapDates());
    }
    private IEnumerator IEnum_SwapDates()
    {
        var screen = new Screen("Swap Assignment Dates", new() { { "Assignment 1", typeof(Assignment) }, { "Assignment 2", typeof(Assignment) } });
        Inputs.LoadScreen(screen, null,
        scrn =>
        {
            return false;
        });
        yield return new WaitUntil(() => screen.Unloaded);

        Assignment a1 = (Assignment)screen["Assignment 1"],
                   a2 = (Assignment)screen["Assignment 2"];

        if (a1.HasStartDate && a2.HasStartDate)
        {
            var tempS = a1.StartDate;
            a1.SetStartDate(a2.StartDate);
            a2.SetStartDate(tempS);
        }
        var tempD = a2.DueDate;
        a1.SetDueDate(a2.DueDate);
        a2.SetDueDate(tempD);
    }
    #endregion
    #endregion
    // Update is called once per frame
    void Update()
    {
        
    }
}
