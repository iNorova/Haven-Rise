using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Weapon")]
public class Weapon : Item
{

    public float attack;
    public float attackSpeed;
    public float magicAttack;
    public float criticalHits;
    public float defence;

    public Weapon(string name)
    {
        this.name = name;
    }

    public Weapon()
    {

    }

}
