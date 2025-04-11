using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newItem", menuName = "baseMaterials/Metal")]
public class Metal : Item, BaseMaterial<Metal>
{

    [Header("Parameters")]
    public float hardness; //3-650
    public float meltingTemperature; //44-3000
    public float density;
    public float thermalConductivity; //0-6000
    public float electricalConductivity; //0-1
    public float thermalInsulation;

    override public string ToString()
    {
        return base.ToString() + "\n" + "hardness: " + hardness.ToString() + "\n" +
            "melting temperature: " + meltingTemperature.ToString() + "\n" +
            "density: " + density.ToString() + "\n" +
            "thermal conductivity: " + thermalConductivity.ToString() + "\n" +
            "electrical conductivity: " + electricalConductivity.ToString() + "\n" +
            "thermal insulation: " + thermalInsulation.ToString();
    }

    public Metal(string name)
    {
        this.name = name;
    }

    public Metal()
    {
       
    }

    override public float getTimeParameter()
    {
        return meltingTemperature;
    }

    public void Add(Metal materialBazowy)
    {
        hardness += materialBazowy.hardness ;
        meltingTemperature += materialBazowy.meltingTemperature;
        density += materialBazowy.density ;
        thermalConductivity += materialBazowy.thermalConductivity;
        electricalConductivity += materialBazowy.electricalConductivity ;
        color += materialBazowy.color;
        conditionerCapacity += materialBazowy.conditionerCapacity;
    }

    public void Multiple(float multiplier)
    {
        hardness  *= multiplier;
        meltingTemperature *= multiplier;
        density *= multiplier;
        thermalConductivity *= multiplier;
        electricalConductivity *= multiplier;
    }

    public void Divide(int i)
    {
        hardness /= i;
        meltingTemperature /= i;
        density /= i;
        thermalConductivity /= i;
        electricalConductivity /= i;
        conditionerCapacity /= i;
        color /= i;
    }

    public void MultipleConditioner(Conditioner uzdatniacz)
    {
        hardness = hardness * uzdatniacz.impact_on_hardness;
        meltingTemperature = meltingTemperature * uzdatniacz.impact_on_melting_temperature;
        density = density * uzdatniacz.impact_on_density;
        thermalConductivity = thermalConductivity * uzdatniacz.impact_on_termal_conductivity;
        electricalConductivity = electricalConductivity * uzdatniacz.impact_on_electrical_conductivity;
        color += uzdatniacz.color;
    }
}
