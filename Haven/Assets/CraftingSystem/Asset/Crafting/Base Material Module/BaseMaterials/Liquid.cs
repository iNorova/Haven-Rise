using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "baseMaterials/Liquid")]
public class Liquid : Item, BaseMaterial<Liquid>
{

    [Header("Parameters")]
    public float density;
    public float processing;
    public float resistance;
    public float magicEfficiency;
    public float healtAbility;
    public float poisonous;
    public float speedInpact;


    override public string ToString()
    {
        return base.ToString() +
            "density: " + density.ToString() + "\n" +
            "processing time: " + processing.ToString() + "\n" +
            "resistance: " + resistance.ToString() + "\n" +
            "magic efficiency: " + magicEfficiency.ToString() + "\n" +
            "poisonous: " + poisonous.ToString() + "/n" +
            "healt ability: " + healtAbility.ToString() + "/n" +
            "speed inpact: " + speedInpact.ToString()+ "/n";
          
    }

    public Liquid(string name)
    {
        this.name = name;
    }

    public Liquid()
    {

    }


    override public float getTimeParameter()
    {
        return processing;
    }

    //Funkcje interfejsu BaseMaterial
    public void Add(Liquid materialBazowy)
    {
        density += materialBazowy.density;
        processing += materialBazowy.processing;
        resistance += materialBazowy.resistance;
        magicEfficiency += materialBazowy.magicEfficiency;
        healtAbility += materialBazowy.healtAbility;
        poisonous += materialBazowy.poisonous;
        speedInpact += materialBazowy.speedInpact;
        color += materialBazowy.color;
        conditionerCapacity += materialBazowy.conditionerCapacity;
        usedConditioner += materialBazowy.usedConditioner;
    }

    public void Multiple(float multiplier)
    {
        density *= multiplier;
        processing *= multiplier;
        resistance *= multiplier;
        magicEfficiency *= multiplier;
        healtAbility *= multiplier;
        poisonous *= multiplier;
        speedInpact *= multiplier;

    }

    public void Divide(int i)
    {
        density /= i;
        processing /= i;
        resistance /= i;
        magicEfficiency /= i;
        healtAbility /= i;
        poisonous /= i;
        speedInpact /= i;
        color /= i;
        conditionerCapacity /= i;
        usedConditioner /= i;
    }

    public void MultipleConditioner(Conditioner uzdatniacz)
    {
        density *= uzdatniacz.impact_on_density;
        processing *= uzdatniacz.impact_on_processing;
        resistance *= uzdatniacz.impact_on_resistance;
        magicEfficiency *= uzdatniacz.impact_on_magic_efect;
        poisonous *= uzdatniacz.impact_on_poison;
        healtAbility *= uzdatniacz.impact_on_regeneration;
        speedInpact *= uzdatniacz.impact_on_density;
        color += uzdatniacz.color;
    }
}
