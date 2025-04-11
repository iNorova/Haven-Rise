using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/TwoHandedWeapon")]
public class TwoHandedWeapon : Item {

    public float attack;
    public float attackSpeed;
    public float magicAttack;
    public float criticalHits;
    public float defence;

    public TwoHandedWeapon(string name)
    {
        this.name = name;
    }

    public TwoHandedWeapon()
    {

    }
}
