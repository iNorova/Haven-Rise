using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mouse : MonoBehaviour {

    public Text text;

    public void updateText(string str)
    {
        text.text = str;
    }
}
