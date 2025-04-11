using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeltCanvas : CraftingCanvas {

	// Use this for initialization
	void Start () {
        ReloadPanels();
	}

    public void Create()
    {

        System.Type[] meltTypes = new System.Type[2];
        meltTypes[0] = typeof(Metal);
        meltTypes[1] = typeof(Conditioner);

        int[] typesQuantity = checkItemsOfTypeInChosenItems(meltTypes);


        Metal[] metals = new Metal[typesQuantity[0]];
        Conditioner[] conditioners = new Conditioner[typesQuantity[1]];
        Object[] otherItems = new Object[typesQuantity[2]];
        int met = 0;
        int uzd = 0;
        int others = 0;
        for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
        {
            if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Metal)))
            {
                metals[met] = (Metal)FindObjectOfType<UiManagment>().chosenItems[i];
                met += 1;
            }
            else if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Conditioner)))
            {
                conditioners[uzd] = (Conditioner)FindObjectOfType<UiManagment>().chosenItems[i];
                uzd += 1;
            }
            else if (FindObjectOfType<UiManagment>().chosenItems[i] != null)
            {
                otherItems[others] = FindObjectOfType<UiManagment>().chosenItems[i];
            }
        }

        if (FindObjectOfType<ParameterConversion>().CanCreateMaterial(metals, conditioners))
        {
            cleanChosenItems(otherItems);
            FindObjectOfType<ParameterConversion>().CreateNewMaterial(metals, conditioners, FindObjectOfType<UiManagment>().exerciseMetod);
        }
        else
        {
            Debug.Log("Wykonanie tej operacji niejest możliwe");
        }
    }

}
