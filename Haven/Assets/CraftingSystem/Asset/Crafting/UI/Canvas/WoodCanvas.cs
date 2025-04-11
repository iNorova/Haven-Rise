using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodCanvas : CraftingCanvas
{

    // Use this for initialization
    void Start()
    {
        ReloadPanels();
    }

    public void Create()
    {

        System.Type[] meltTypes = new System.Type[2];
        meltTypes[0] = typeof(Wood);
        meltTypes[1] = typeof(Conditioner);

        int[] typesQuantity = checkItemsOfTypeInChosenItems(meltTypes);


        Wood[] woods = new Wood[typesQuantity[0]];
        Conditioner[] uzdatniacze = new Conditioner[typesQuantity[1]];
        Object[] otherItems = new Object[typesQuantity[2]];
        int met = 0;
        int uzd = 0;
        int others = 0;
        for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
        {
            if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Wood)))
            {
                woods[met] = (Wood)FindObjectOfType<UiManagment>().chosenItems[i];
                met += 1;
            }
            else if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Conditioner)))
            {
                uzdatniacze[uzd] = (Conditioner)FindObjectOfType<UiManagment>().chosenItems[i];
                uzd += 1;
            }
            else if (FindObjectOfType<UiManagment>().chosenItems[i] != null)
            {
                otherItems[others] = FindObjectOfType<UiManagment>().chosenItems[i];
            }
        }

        if (FindObjectOfType<ParameterConversion>().CanCreateMaterial(woods, uzdatniacze))
        {
            cleanChosenItems(otherItems);
            FindObjectOfType<ParameterConversion>().CreateNewMaterial(woods, uzdatniacze, FindObjectOfType<UiManagment>().exerciseMetod);
        }
        else
        {
            Debug.Log("You can't do this operation");
        }
    }


}

