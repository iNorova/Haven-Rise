using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Recipe : ScriptableObject {

    public Sprite image;
    [TextArea(7,8)]
    public string description;
    public Color defaultColor;

    [Header("Recipe Restrictions")]
    public int lvl;
    public float exp;
    public float time;
    public int materialCapacity;
    public Object[] requiredItems;


    public override string ToString()
    {
        string response = name + "\n" + lvl + "\n" + description + "\n" + materialCapacity + "\n Items: \n";
        for (int i = 0; i < requiredItems.Length; i++)
        {
            response += requiredItems[i].name + "\n";
        }

        return response;
    }

    private void OnEnable()
    {
        if (image == null)
        {
            image = FindObjectOfType<Configuration>().defaultSprite;
        }
    }
}
