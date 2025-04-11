using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "Conditioner")]
public class Conditioner : Item {

    [Header("Parameters")]
    public float impact_on_hardness; //0-1
    public float impact_on_density; //0-1
    public float impact_on_melting_temperature;
    public float impact_on_termal_conductivity; //0-1
    public float impact_on_electrical_conductivity; //0-1
    public float impact_on_termal_permeability;
    public float impact_on_flexibility;
    public float impact_on_strenght;
    public float impact_on_processing;
    public float impact_on_magic_efect;
    public float impact_on_resistance;
    public float impact_on_regeneration;
    public float impact_on_poison;


}
