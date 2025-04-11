using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Legs")]
public class Legs : Item {

    public float defence;
    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;

    public Legs(string name)
    {
        this.name = name;
    }

    public Legs()
    {

    }
}
