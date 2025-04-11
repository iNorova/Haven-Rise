using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewWand", menuName = "Recipe/Wand", order = 1)]
public class WandRecipe : Recipe {

    [Header("Parameters")]
    public float multiplier_earthAttack;
    public float multiplier_fireAttack;
    public float multiplier_waterAttack;
    public float multiplier_windAttack;
}
