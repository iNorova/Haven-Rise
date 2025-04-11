using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ready : ScriptableObject
{

    public bool ready = false;
    public Object item;
    public float executionTime;
    public float progress = 0;
    public Slider slider;
    public Text progressText;

}
