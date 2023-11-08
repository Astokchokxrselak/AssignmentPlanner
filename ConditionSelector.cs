using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.Extensions;
using Common.UI;

using PText = TMPro.TextMeshProUGUI;
public class ConditionSelector : MonoBehaviour
{
    public GameObject baseCondition;
    private Toggle[] toggles;
    private PText pageNumber;
    // Start is called before the first frame update
    void Start()
    {
        transform.TryFindComponentInChildren<PText>("PageNumber", out pageNumber);
        LoadPage();
    }

    public int currentPage = 0;
    private int maxPage;
    void LoadPage()
    {
        var hls = GetComponentsInChildren<HorizontalLayoutGroup>(); // atm defaulting to 2x3 selection grid
        foreach (var hl in hls)
        {
            if (hl.transform.childCount > 0)
            {
                for (int i = 0; i < hl.transform.childCount; i++)
                {
                    Destroy(hl.transform.GetChild(i).gameObject);
                }
            }
        }

        toggles = new Toggle[Groups.ConditionsByName.Count];
        maxPage = (toggles.Length - 1) / (3 * 2);
        var conditionNames = Groups.ConditionsByName.Keys.ToList();
        for (int i = currentPage * 3 * 2, p = 0; i < Mathf.Min((currentPage + 1) * 3*2, toggles.Length); i++, p++)
        {
            var hl = hls[p / 2];
            var condition = Instantiate(baseCondition, hl.transform);
            toggles[i] = condition.GetComponentInChildren<Toggle>();
            condition.GetComponentInChildren<Text>().text = conditionNames[i];
        }
        pageNumber.text = string.Format("{0} / {1}", currentPage + 1, maxPage + 1);
    }
    public void NextPage(Object sender)
    {
        currentPage++;
        currentPage %= maxPage + 1;
        LoadPage();
    }
    public void PreviousPage(Object sender)
    {
        currentPage--;
        currentPage %= maxPage + 1;
        LoadPage();
    }
    public void NextPageUpdateButton(Object target)
    {
        (target as Button).interactable = currentPage != maxPage;
    }
    public void PreviousPageUpdateButton(Object target)
    {
        (target as Button).interactable = currentPage != 0;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
