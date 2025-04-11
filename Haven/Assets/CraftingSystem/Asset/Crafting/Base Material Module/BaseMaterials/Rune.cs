using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "baseMaterials/Rune")]
public class Rune : Item, BaseMaterial<Rune>{

    [Header("Parameters")]
    public float earth;
    public float fire;
    public float wind;
    public float water;
    public float craftTime;

    override public string ToString()
    {
        return base.ToString() + "\n" +
            "Earth: " + earth.ToString() + "\n" +
            "Fire: " + fire.ToString() + "\n" +
            "Wind: " + wind.ToString() + "\n" +
            "Water: " + water.ToString();
    }



    public Rune(string name)
    {
        this.name = name;
    }

    public Rune()
    {

    }

    override public float getTimeParameter()
    {
        return craftTime;
    }


    public void Add(Rune materialBazowy)
    {

        earth += materialBazowy.earth;
        fire += materialBazowy.fire;
        wind += materialBazowy.wind;
        water += materialBazowy.water;
        color += materialBazowy.color;
        usedConditioner += materialBazowy.usedConditioner;
        conditionerCapacity += materialBazowy.conditionerCapacity;
    }

    public void Multiple(float multiplier)
    {
        earth *= multiplier;
        fire *= multiplier;
        wind *= multiplier;
        water *= multiplier;
    }

    public void Divide(int i)
    {
        fire /= i;
        wind /= i;
        water /= i;
        earth /= i;
        color /= i;
        conditionerCapacity /= i;
        usedConditioner /= i;
    }
    
    public void MultipleConditioner(Conditioner uzdatniacz)
    {
        earth *= uzdatniacz.impact_on_hardness;
        wind *= uzdatniacz.impact_on_density;
        fire *= uzdatniacz.impact_on_strenght;
        water *= uzdatniacz.impact_on_processing;
        color += uzdatniacz.color;
    }
}
