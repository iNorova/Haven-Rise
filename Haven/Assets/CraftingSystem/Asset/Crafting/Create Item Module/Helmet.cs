using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Helmet")]
public class Helmet : Item {

    public float defence;
    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;

    public Helmet(string name)
    {
        this.name = name;
    }

    public Helmet()
    {

    }
}
