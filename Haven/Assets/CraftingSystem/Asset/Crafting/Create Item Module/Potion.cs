using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Potion")]
public class Potion : Item {

    public float speed;
    public float posionous;
    public float HPrecovery;
    public float MPrecovery;
    public float resistance;
    public float magicResistance;
    public float timeAction;

    public Potion(string name)
    {
        this.name = name;
    }

    public Potion()
    {

    }
}
