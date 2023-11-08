using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.Helpers;

using TMPro;
using Text = TMPro.TextMeshProUGUI;

public class IntervalTimerMain : MonoBehaviour
{
    private SFXData _sfx;

    // Frmaewowrk
    // Interval Timer
    // Step 1: Countdown
    // We have a set time to countdown from for each task
    private int ticks; // in seconds
    // We have a list of tasks we iterate through and give a timer to
    private int taskIndex = 0;
    public string[] tasks;
    public int[] countdownTimes;
    public bool paused = false;
    // We have a set number of times we iterate over this list of tasks

    [Header("UI Objects")]
    public Text timerDisplay;
    public Button switchButton, pauseButton;
    public Text taskDisplay;
    // We may use IEnumerator
    private void _Start()
    {
        Application.runInBackground = true;
        TryGetComponent(out _sfx);
        StartCoroutine(_Start_IEnum());
    }


    private readonly WaitForSecondsRealtime OneSecond = new WaitForSecondsRealtime(1);
    private IEnumerator _Start_IEnum()
    {
        while (true)
        {
            for (taskIndex = 0; taskIndex < tasks.Length; taskIndex++)
            {

                var task = tasks[taskIndex];
                var countdownTime = countdownTimes[taskIndex];
                while (ticks < countdownTime)
                {
                    task = tasks[taskIndex]; // update from editor
                    countdownTime = countdownTimes[taskIndex]; // update from editor
                    
                    ticks++;
                    taskDisplay.text = task;
                    timerDisplay.text = TimeSpan.FromSeconds(countdownTime - ticks).ToString(@"hh\:mm\:ss");
                    yield return new WaitUntil(() => !paused);
                    yield return OneSecond;
                }
                _sfx.PlaySFX("Beep");
                const int PostTicksPerBeep = 1; // beep every n seconds
                for (int postTicks = 0; !Application.isFocused; postTicks++)
                {
                    if (postTicks % PostTicksPerBeep == 0)
                    {
                        _sfx.PlaySFX("Beep");
                    }
                    yield return OneSecond;
                }
                yield return new WaitUntil(() => Application.isFocused); // incase we are finishing something up
                ResetTimer();
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        switchButton.onClick.AddListener(() =>
        {
            taskIndex++;
            ResetTimer();
        });
        pauseButton.onClick.AddListener(() =>
        {
            paused = !paused;
        });
        _Start();
    }
    private void ResetTimer()
    {
        ticks = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
