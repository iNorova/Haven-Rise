using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Gloves")]
public class Gloves : Item {

    public float defence;
    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;
    public float speedAtack;

    public Gloves(string name)
    {
        this.name = name;
    }

    public Gloves()
    {

    }
}
