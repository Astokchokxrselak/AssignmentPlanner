using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.Helpers;
using Common.Extensions;

using Text = TMPro.TextMeshProUGUI;
using System.Linq;
using System.Drawing.Printing;

public delegate bool GroupCondition(Assignment a, Dictionary<string, object> args = null);
public enum GCOperator
{
    NONE,
    BUF, // placeholder
    AND, // C && C
    OR, // C || C
}
public interface ITriad {
    public abstract string ConditionName { get; init; }
    public abstract GCOperator GCOperator { get; init; }
    public abstract bool Invert { get; init; }
    public abstract bool GetCondition(Assignment a);
}
/*[Serializable]
public struct GroupTriad : ITriad {
    public string ConditionName { get; init; }
    public GCOperator GCOperator { get; init; }
    public bool Invert { get; init; }
    public bool GetCondition(Assignment a) {
        return !Groups.ConditionsByName[ConditionName](a);
    }
    public GroupTriad(string cname, GCOperator opp, bool inv) {
        ConditionName = cname;
        GCOperator = opp;
        Invert = inv;
    }
}*/
[Serializable]
public struct GroupTriad : ITriad {
    public string ConditionName { get; init; }
    public GCOperator GCOperator { get; init; }
    public bool Invert { get; init; }
    public Dictionary<string, object> Data;
    public bool GetCondition(Assignment a) {
        return !Groups.ConditionsByName[ConditionName](a, Data);
    }
    public GroupTriad(string cname, GCOperator opp, bool inv) {
        ConditionName = cname;
        GCOperator = opp;
        Invert = inv;
        Data = new();
    }
}
[Serializable]
public class Group
{
    public string name;
    public List<Assignment> assignments;
    public List<GroupTriad> conditions;
    public bool IsActive() 
    {
        for (int j = 0; j < assignments.Count; j++)
        {
            var a = assignments[j];
            if (!IsAssignmentActive(a))
            {
                return true;
            }
        }
        return false;
    }
    public bool IsAssignmentActive(Assignment a)
    {
        this.print(a.Title);
        if (conditions.Count == 0) {
            return true;
        }
        var con = Groups.ConditionsByName[conditions[0].ConditionName];
        bool value = con(a) ^ conditions[0].Invert;
        this.print("VAL 1: " + value + "w/ CON: " + conditions[0]);
        for (int i = 1; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            GCOperator opp = condition.GCOperator;

            bool v = con(a) ^ condition.Invert;
            switch (opp)
            {
                case GCOperator.AND:
                    value = value && v;
                    break;
                case GCOperator.OR:
                    value = value || v;
                    break;
            }
            this.print("VAl " + (i + 1) + ": " + value + "w/ CON: " + conditions[i]);
        }
        return value;
    }
    private class AssignmentComparer : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            Assignment ax = x as Assignment, ay = y as Assignment;
            DateTime axd, ayd;
            axd = ax.HasStartDate ? ax.StartDate : ax.DueDate;
            ayd = ay.HasStartDate ? ay.StartDate : ay.DueDate;
            if (axd == ayd)
            {
                return 0;
            }
            else
            {
                return axd.CompareTo(ayd);
            }
        }
    }
    public Group(string name, List<Assignment> assignments)
    {
        this.name = name;
        this.assignments = new(assignments);
        this.conditions = new();
    }
    public Group(string name, List<Assignment> assignments, List<GroupTriad> conditions) : this(name, assignments)
    {
        this.conditions = conditions;
        /*
        List<GroupCondition> tempCons = new List<GroupCondition>() { () => true };
        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (condition.opp == GCOperator.AND)
            {
                tempCons.Add(() => (condition.con() ^ condition.invert));
            }
            else if (condition.opp == GCOperator.OR)
            {
                GroupCondition f = () => (condition.con() ^ condition.invert);
                func = f;
            }
        }
        Debug.Log(func());
        */
    }
}
public class Groups : MonoBehaviour
{
    public static Groups Instance;
    public static Group activeGroup;
    public static Group focusedGroup;
    public List<Group> groups;
    public static int GroupCount => Groups.Instance.groups.Count;
    public static Group GetGroup(int index) => Groups.Instance.groups[index];
    public void _InitializeAddButton() {
        print("go foro steven");
        addButton.onClick.AddListener(() => {
            CommonGameManager.Coroutine(IEnum_OnAddGroup());
        });
    }
    private IEnumerator IEnum_OnAddGroup(int mode = 1, int groupID = -1) 
    {
        // TODO: Menu style assignment-selector to add assignments to a group intuitively
        // Select multiple from a scrolling list
        string name = groupID == -1 ? default : groups[groupID].name;
        switch (mode) 
        {
            case 1:
                var screen1 = new Screen("New Group", new() {
                    { new InputDescription("Name"), typeof(string) }
                });
                Inputs.LoadScreen(screen1, scrn => {
                    name = screen1["Name"].ToString();
                },
                scrn => {
                    return false;
                });

                yield return new WaitUntil(() => screen1.Unloaded);
                if (screen1.backed) {
                    break;
                }
                goto case 2;
            case 2:
                Transform oldMain = Main.mainScreen;
                yield return Main.RequestAddConditions(null, name);
                if (DraggableMain.quitDisabled) {
                    Main.SetMainScreenAndShow(oldMain);
                    yield break;
                }
                var conditions = SaveGroupCondition.Parse();

                var ng = new Group(name, new(), conditions);
                groups.Add(ng);

                SetFocusedGroupAndShow(ng);
                break;
        }
    }

    public static Dictionary<string, GroupCondition> ConditionsByName;
    private string GetConditionName(GroupCondition condition)
    {
        foreach (var pair in ConditionsByName)
        {
            if (pair.Value == condition)
            {
                return pair.Key;
            }
        } return null;
    }
    public string GetConditionString(string gc, bool invert)
    {
        string @out = "";
        if (invert)
        {
            @out += "NOT ";
        }
        @out += gc + " ";
        return @out;
    }
    public string GetConditionsString(Group group)
    {
        var conditions = group.conditions;
        var firstCondition = conditions[0];
        var @out = GetConditionString(firstCondition.ConditionName, firstCondition.Invert);
        for (int i = 1; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            switch (condition.GCOperator)
            {
                case GCOperator.AND:
                    @out += "AND ";
                    break;
                case GCOperator.OR:
                    @out += "OR ";
                    break;
            }
            @out += GetConditionString(condition.ConditionName, condition.Invert);
        }
        return @out;
    }
    // Start is called before the first frame update
    private void InitializeActiveGroup()
    {
        activeGroup = new Group("Active Group",
            new(),
            new()
            {
            }
        );
    }
    public static Dictionary<string, Group> GenericGroups; 
    void Awake()
    {
        ConditionsByName = new()
        {
            { "Mondays", (a, args) => {
                print(a.GenericStartDate + ", DOW: " + a.GenericStartDate.DayOfWeek);
                return a.GenericStartDate.DayOfWeek == DayOfWeek.Monday;
                }
            },
            { "Tuesdays", (a, args) => {
                print(a.GenericStartDate + ", DOW: " + a.GenericStartDate.DayOfWeek);
                return a.GenericStartDate.DayOfWeek == DayOfWeek.Tuesday;
                } },
            { "Wednesdays", (a, args) => {
                print(a.GenericStartDate + ", DOW: " + a.GenericStartDate.DayOfWeek);
                return a.GenericStartDate.DayOfWeek == DayOfWeek.Wednesday;
                }  },
            { "Thursdays", (a, args) => {
                print(a.GenericStartDate + ", DOW: " + a.GenericStartDate.DayOfWeek);
                return a.GenericStartDate.DayOfWeek == DayOfWeek.Thursday;
                } },
            { "Fridays", (a, args) => a.GenericStartDate.DayOfWeek == DayOfWeek.Friday },
            { "Saturdays", (a, args) => a.GenericStartDate.DayOfWeek == DayOfWeek.Saturday },
            { "Sundays", (a, args) => a.GenericStartDate.DayOfWeek == DayOfWeek.Sunday },
            { "Weekdays", (a, args) => MathHelper.Between((int)a.GenericStartDate.DayOfWeek, (int)DayOfWeek.Monday, (int)DayOfWeek.Saturday) },
            { "Weekends", (a, args) => a.GenericStartDate.DayOfWeek == DayOfWeek.Saturday || a.GenericStartDate.DayOfWeek == DayOfWeek.Sunday },
            { "Mornings", (a, args) => MathHelper.Between((float)a.GenericStartDate.TimeOfDay.TotalHours, 6, 12) },
            { "Afternoons", (a, args) => MathHelper.Between((float)a.GenericStartDate.TimeOfDay.TotalHours, 12, 17) },
            { "Evenings", (a, args) => MathHelper.Between((float)a.GenericStartDate.TimeOfDay.TotalHours, 17, 19) },
            { "Nights", (a, args) => MathHelper.Between((float)a.GenericStartDate.TimeOfDay.TotalHours, 19, 24) || MathHelper.Between((float)a.GenericStartDate.TimeOfDay.TotalHours, 0, 6) },
            { "GroupIsActive", (a, args) => GetGroup((int)args["GROUPINDEX"]).IsActive() } // we use the index in the dropdown and display the name in the editor
        };

        var groups = new List<Group>() { 
            new Group("Weekdays", new(), new() {
                new("Weekdays", GCOperator.BUF, false)
            }),
            new Group("Weekends", new(), new() {
                new("Weekends", GCOperator.BUF, false)
            }),
            new Group("MWF", new(), new() {
                new("Mondays", GCOperator.BUF, false),
                new("Wednesdays", GCOperator.OR, false),
                new("Fridays", GCOperator.OR, false)
            }),
            new Group("MSFT", new(), new() {
                new("Weekdays", GCOperator.BUF, false),
                new("Mondays", GCOperator.AND, true),
                new("Fridays", GCOperator.AND, true)
            })
        };
        GenericGroups = groups.ToDictionary(k => k.name);

        
        Assignments.OnAssignmentsRebuilt += () => RebuildGroups();

        Instance = this;
        InitializeActiveGroup();

        groups.Add(new("Freedays",
            new(),
            new()
            {
                new("Tuesdays", GCOperator.BUF, false),
                new("Wednesdays", GCOperator.AND, false),
                new("Thursdays", GCOperator.AND, false),
            }
        ));

        checkTimer = new Timer(CheckInterval, CheckInterval);
        
        _InitializeAddButton();
    }

    private const float CheckInterval = 3f;

    private Timer checkTimer;
    // Update is called once per frame
    void Update()
    {
        /*if (checkTimer.IncrementHit(true, true, true))
        {
            RebuildGroups();
        }*/
    }

    public Transform ActiveGroups, InactiveGroups;
    public static void DestroyAllGroups()
    {
        foreach (Transform child in Instance.ActiveGroups)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in Instance.InactiveGroups)
        {
            Destroy(child.gameObject);
        }
    }
    public GameObject baseGroup; // , basePersistentAssignment, baseRandomAssignment, basePersistentReminder;
    public Button addButton;
    public static void RebuildGroups()
    {
        // Instance.StartCoroutine(IEnum_RebuildGroups());
        DestroyAllGroups();
        activeGroup.assignments.Clear();

        Instance.InstantiateGroup(activeGroup, Instance.ActiveGroups);
        for (int i = 0; i < Instance.groups.Count; i++)
        {
            var group = Instance.groups[i];
            bool anyActive = false;
            for (int j = 0; j < group.assignments.Count; j++)
            {
                var a = group.assignments[j];
                if (!group.IsAssignmentActive(a))
                {
                    a.SetDone(true);
                }
                else
                {
                    activeGroup.assignments.Add(a);
                    print(a.Title + " / " + a.GenericStartDate.DayOfWeek);
                    anyActive = true;
                }
            }
            if (anyActive)
            {
                Instance.InstantiateGroup(group, Instance.ActiveGroups);
            }
            else
            {
                Instance.InstantiateGroup(group, Instance.InactiveGroups);
            }
        }
    }
    private static IEnumerator IEnum_RebuildGroups()
    {
        yield break;
        /*DestroyAllGroups();
        activeGroup.assignments.Clear();

        Instance.InstantiateGroup(activeGroup, Instance.ActiveGroups);
        for (int i = 0; i < Instance.groups.Count; i++)
        {
            var group = Instance.groups[i];
            if (group.IsActive())
            {
                Instance.InstantiateGroup(group, Instance.ActiveGroups);
                activeGroup.assignments.AddRange(group.assignments);
            }
            else
            {
                Instance.InstantiateGroup(group, Instance.InactiveGroups);
            }
        }
        yield break;*/
    }
    private void InstantiateGroup(Group g, Transform parent)
    {
        if (Main.FocusedOnGroup)
        {
            return;
        }
        var group = Instantiate(baseGroup, parent).transform;
        Text title = group.FindComponent<Text>("Title"),
             id = group.FindComponent<Text>("GroupID"), 
             conditions = group.FindComponent<Text>("GroupConditions");
        Button focus = group.FindComponent<Button>("Focus");
        title.text = g.name;
        id.text = "Group ID: " + (groups.IndexOf(g) + 1);
        if (g.conditions.Count > 1)
            conditions.text = "Group Conditions: " + GetConditionsString(g);
        else if (g.conditions.Count == 1)
            conditions.text = "Group Condition: " + GetConditionsString(g);
        else
            conditions.text = "Group Condition: Always Active";
        focus.onClick.AddListener(() =>
        {
            SetFocusedGroupAndShow(g);
        });
        // conditions.text = g.conditions.Aggregate(c1, c2 => c1.con);
    }
    public static void SetFocusedGroup(Group group) {
        focusedGroup = group;
    }
    public static void SetFocusedGroupAndShow(Group group) {
        SetFocusedGroup(group);
        Main.InitializeFocusedGroup(group);
    }
}
