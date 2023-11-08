using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common.UI
{
    public class IsolateChildOnStart : MonoBehaviour
    {
        public int childIndex;
        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i == childIndex);
            }
        }
    }
}