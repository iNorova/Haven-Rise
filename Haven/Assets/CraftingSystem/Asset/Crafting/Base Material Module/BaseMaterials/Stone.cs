using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "baseMaterials/Stone")]
public class Stone : Item, BaseMaterial<Stone>
{

    [Header("Parameters")]
    public float hardness;
    public float density;
    public float strenght;
    public float processing;
    public float flexibility;
    public float resistance;
    public float magicEfficiency;


    override public string ToString()
    {
        return base.ToString() + "\n" + "hardness: " + hardness.ToString() + "\n" +
            "density: " + density.ToString() + "\n" +
            "strenght: " + strenght.ToString() + "\n" +
            "processing time: " + processing.ToString() + "\n" +
            "flexibility: " + flexibility.ToString() + "\n" +
            "resistance: " + resistance.ToString() + "\n" +
            "magic efficiency: " + magicEfficiency.ToString() + "\n";
    }

    public Stone(string name)
    {
        this.name = name;
    }

    public Stone()
    {

    }


    override public float getTimeParameter()
    {
        return processing;
    }

    //Funkcje interfejsu BaseMaterial
    public void Add(Stone materialBazowy)
    {
        hardness += materialBazowy.hardness;
        density += materialBazowy.density;
        strenght += materialBazowy.strenght;
        processing += materialBazowy.processing;
        flexibility += materialBazowy.flexibility;
        resistance += materialBazowy.resistance;
        magicEfficiency += materialBazowy.magicEfficiency;
        color += materialBazowy.color;
        conditionerCapacity += materialBazowy.conditionerCapacity;
        usedConditioner += materialBazowy.usedConditioner;
    }

    public void Multiple(float multiplier)
    {
        hardness *= multiplier;
        density *= multiplier;
        strenght *= multiplier;
        processing *= multiplier;
        flexibility *= multiplier;
        resistance *= multiplier;
        magicEfficiency *= multiplier;

    }

    public void Divide(int i)
    {
        hardness /= i;
        density /= i;
        strenght /= i;
        processing /= i;
        flexibility /= i;
        resistance /= i;
        magicEfficiency /= i;
        color /= i;
        conditionerCapacity /= i;
        usedConditioner /= i;
    }

    public void MultipleConditioner(Conditioner conditioner)
    {
        hardness *= conditioner.impact_on_hardness; //3-650
        density *= conditioner.impact_on_density;
        strenght *= conditioner.impact_on_strenght;
        processing *= conditioner.impact_on_processing;
        flexibility *= conditioner.impact_on_flexibility;
        resistance *= conditioner.impact_on_resistance;
        magicEfficiency *= conditioner.impact_on_magic_efect;
        color += conditioner.color;
    }
}
