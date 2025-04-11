using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe/Ring", order = 1)]
public class RingRecipe : Recipe {

    [Header("Parameters")]
    public float multiplier_attack;
    public float multiplier_attackSpeed;
    public float multiplier_magicAttack;
    public float multiplier_criticalHits;
    public float multiplier_defence;

    public float multiplier_earthAttack;
    public float multiplier_fireAttack;
    public float multiplier_waterAttack;
    public float multiplier_windAttack;
                 
    public float multiplier_magicDefence;
    public float multiplier_fireDefence;
    public float multiplier_earthDefence;
    public float multiplier_frostDefence;
    public float multiplier_windDefence;
}
