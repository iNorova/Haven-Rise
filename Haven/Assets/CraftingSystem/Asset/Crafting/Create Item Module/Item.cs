using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Item : ScriptableObject{

    public Sprite image;

    public Color color;

    [TextArea(7,8)]
    public string description;
    [Space]
    public int lvl;
    public float exp;

    public float usedConditioner;
    public int conditionerCapacity;

    virtual public float getTimeParameter()
    {
        return 1;
    }

    private void OnEnable()
    {
        if (image == null)
        {
            image = FindObjectOfType<Configuration>().defaultSprite;
        }
    }

    public override string ToString()
    {
        string response = name + "\n" + lvl + "\n" + description;
        return response;
    }

}
