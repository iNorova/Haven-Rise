using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Boots")]
public class Boots : Item {
    public float defence;
    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;
    public float speed;

    public Boots(string name)
    {
        this.name = name;
    }

    public Boots()
    {

    }
}
