using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Backpack : MonoBehaviour {

    public List<Object> items = new List<Object>();

    public float crafting_exp = 0;

    public void addCraftingExp(float quantity)
    {
        crafting_exp += quantity;
    }

}
