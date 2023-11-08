using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common;
public class ReloadLayoutGroupOnStart : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        CommonGameManager.Coroutine(_EnableDisable(gameObject));
    }
    private IEnumerator _EnableDisable(GameObject obj) {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
   }
}
