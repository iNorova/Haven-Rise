using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Recipe/TwoHandedWeapon", order = 1)]
public class TwoHandedWeaponRecipe : Recipe {

    [Header("Parameters")]
    public float multiplier_speed_attack;
    public float multiplier_physical_attack;
    public float multiplier_magic_attack;
    public float multiplier_crytical_damage;
    public float multiplier_defence;
}
