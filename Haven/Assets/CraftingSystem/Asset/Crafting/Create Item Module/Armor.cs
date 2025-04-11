using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Armor")]
public class Armor : Item {

    public float defence;
    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;

    public Armor(string name)
    {
        this.name = name;
    }

    public Armor()
    {

    }
}
