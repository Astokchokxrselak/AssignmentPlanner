using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

using Common.Helpers;
using Common.Extensions;

using InputField = TMPro.TMP_InputField;
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    private static string planName;
    public bool autosave;

    private void Awake()
    {
        instance = this;
        var inputField = ComponentHelper.FindObjectOfName<InputField>("EnterSaveFileName");
        inputField.text = planName;
        void func(string tex)
        {
            planName = tex;
        }
        inputField.onEndEdit.AddListener(func);
        inputField.onSubmit.AddListener(func);

        planName = "Default";
#if !UNITY_EDITOR // we will use planName from inspector otherwise
        if (PlayerPrefs.HasKey("LastLoadedFile")) 
        {
            planName = PlayerPrefs.GetString("LastLoadedFile", planName);
            LoadData();
        }
#endif
    }
    public void ChangePlanName(string text)
    {
        planName = text;
    }
    public void SaveData()
    {
        if (!Directory.Exists("Plans"))
            Directory.CreateDirectory("Plans");

        BinaryFormatter bin = new();

        using FileStream saveFile = File.Create("Plans/" + planName + ".bin");
        bin.Serialize(saveFile, Groups.Instance.groups);

        print("Game Saved to " + Directory.GetCurrentDirectory() + "\\Plans\\" + planName + ".bin");
        PlayerPrefs.SetString("LastLoadedFile", planName);
    }
    public void SaveData(Group[] data)
    {
        if (!Directory.Exists("Plans"))
            Directory.CreateDirectory("Plans");

        BinaryFormatter bin = new();

        using FileStream saveFile = File.Create("Plans/" + planName + ".bin");
        bin.Serialize(saveFile, data);

        print("Game Saved to " + Directory.GetCurrentDirectory() + "\\Plans\\" + planName + ".bin");
        PlayerPrefs.SetString("LastLoadedFile", planName);
    }
    public void LoadData()
    {
        BinaryFormatter bin = new();
        
        if (!File.Exists("Plans/" + planName + ".bin"))
        {
            SaveData(new Group[0]);
        }
        using FileStream loadFile = File.OpenRead("Plans/" + planName + ".bin");

        var groups = (List<Group>)bin.Deserialize(loadFile);

        Groups.Instance.groups = groups;
        Groups.RebuildGroups();
        Assignments.Reaccumulate();
        print("----Assignments/Groups Loaded----");

        int i = 0;
        foreach (Group group in groups) {
            print(string.Format("----GROUP {0}----", ++i));
            print(string.Format("----# OF ASSIGNMENTS {0}----", group.assignments.Count));
        }
        PlayerPrefs.SetString("LastLoadedFile", planName);
    }
}
