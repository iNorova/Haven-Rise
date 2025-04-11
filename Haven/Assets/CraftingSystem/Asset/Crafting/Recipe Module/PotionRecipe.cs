using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe/Potion", order = 1)]
public class PotionRecipe : Recipe {

    public float multiplier_speed;
    public float multiplier_posionous;
    public float multiplier_HPrecovery;
    public float multiplier_MPrecovery;
    public float multiplier_resistance;
    public float multiplier_magicResistance;
    public float multiplier_timeAction;
}
