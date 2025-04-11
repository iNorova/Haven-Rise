using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Wand")]
public class Wand : Item {

    public float earthAttack;
    public float fireAttack;
    public float waterAttack;
    public float windAttack;

    public Wand(string name)
    {
        this.name = name;
    }

    public Wand()
    {

    }
}
