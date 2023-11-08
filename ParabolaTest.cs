using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;
using Common.Helpers;

public class ParabolaTest : MobileEntity
{
    public int time;
    int increment = 2;
    int max = 100;
    void FixedUpdate()
    {
        time = (time + increment) % (max + increment);
        var value = time / 100f;
        VelocityX = 1;
        VelocityY = 2 * Mathf.Cos(value);
    }
}
