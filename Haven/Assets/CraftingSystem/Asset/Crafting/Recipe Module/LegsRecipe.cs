using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Recipe/Legs", order = 1)]
public class LegsRecipe : Recipe {

    [Header("Parameters")]
    public float multiplier_defence;
    public float multiplier_magicDefence;
    public float multiplier_fireDefence;
    public float multiplier_earthDefence;
    public float multiplier_frostDefence;
    public float multiplier_windDefence;
}
