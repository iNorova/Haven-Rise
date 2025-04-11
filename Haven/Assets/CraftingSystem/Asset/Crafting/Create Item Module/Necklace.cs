using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Necklace")]
public class Necklace : Item {


    public float attack;
    public float attackSpeed;
    public float magicAttack;
    public float criticalHits;
    public float defence;

    public float earthAttack;
    public float fireAttack;
    public float waterAttack;
    public float windAttack;

    public float magicDefence;
    public float fireDefence;
    public float earthDefence;
    public float frostDefence;
    public float windDefence;

    public Necklace(string name)
    {
        this.name = name;
    }

    public Necklace()
    {

    }
}
