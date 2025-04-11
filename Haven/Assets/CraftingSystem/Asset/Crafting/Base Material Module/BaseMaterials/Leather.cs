using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "baseMaterials/Leather")]
public class Leather : Item, BaseMaterial<Leather>
{
    [Header("Parameters")]
    public float hardness;
    public float density;
    public float strenght;
    public float processing;
    public float flexibility;
    public float resistance;
    public float magicEfficiency;
    public float temperatureConductivity;


    override public string ToString()
    {
        return base.ToString() + "\n" + "hardness: " + hardness.ToString() + "\n" +
            "density: " + density.ToString() + "\n" +
            "strenght: " + strenght.ToString() + "\n" +
            "processing time: " + processing.ToString() + "\n" +
            "flexibility: " + flexibility.ToString() + "\n" +
            "resistance: " + resistance.ToString() + "\n" +
            "magic efficiency: " + magicEfficiency.ToString() + "\n" +
            "temperature conductivity: " + temperatureConductivity.ToString() + "\n";
    }

    public Leather(string name)
    {
        this.name = name;
    }

    public Leather()
    {

    }


    override public float getTimeParameter()
    {
        return processing;
    }

    //Funkcje interfejsu BaseMaterial
    public void Add(Leather materialBazowy)
    {
        hardness += materialBazowy.hardness;
        density += materialBazowy.density;
        strenght += materialBazowy.strenght;
        processing += materialBazowy.processing;
        flexibility += materialBazowy.flexibility;
        resistance += materialBazowy.resistance;
        magicEfficiency += materialBazowy.magicEfficiency;
        temperatureConductivity += materialBazowy.magicEfficiency;
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
        temperatureConductivity *= multiplier;

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
        temperatureConductivity /= i;
    }

    public void MultipleConditioner(Conditioner uzdatniacz)
    {
        hardness *= uzdatniacz.impact_on_hardness; //3-650
        density *= uzdatniacz.impact_on_density;
        strenght *= uzdatniacz.impact_on_strenght;
        processing *= uzdatniacz.impact_on_processing;
        flexibility *= uzdatniacz.impact_on_flexibility;
        resistance *= uzdatniacz.impact_on_resistance;
        magicEfficiency *= uzdatniacz.impact_on_magic_efect;
        temperatureConductivity *= uzdatniacz.impact_on_termal_permeability;
        color += uzdatniacz.color;
    }
}
