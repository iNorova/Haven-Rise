using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodItemCanvas : CraftingCanvas {

    void Start()
    {
        ReloadPanels();
    }


    public void Create()
    {
        int quantityWoods = 0;
        Recipe recipe = FindObjectOfType<UiManagment>().chosenRecipe;

        for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
        {
            if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Wood)))
            {
                quantityWoods += 1;
            }
        }

        if (recipe != null && FindObjectOfType<CreateItems>().checkRecipe(recipe, FindObjectOfType<UiManagment>().chosenItems, typeof(Wood)))
        {

            int wood = 0;
            Wood[] woods = new Wood[quantityWoods];
            for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
            {
                if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Wood)))
                {
                    woods[wood] = (Wood)FindObjectOfType<UiManagment>().chosenItems[i];
                    FindObjectOfType<UiManagment>().chosenItems[i] = null;
                    wood += 1;
                }
            }

            for (int i = 0; i < recipe.requiredItems.Length; i++)
            {
                for (int j = 0; j < FindObjectOfType<UiManagment>().chosenItems.Count; j++)
                {
                    if (FindObjectOfType<UiManagment>().chosenItems[j] != null && recipe.requiredItems[i].name == FindObjectOfType<UiManagment>().chosenItems[j].name)
                    {
                        FindObjectOfType<UiManagment>().chosenItems[j] = null;
                    }
                }
            }

            FindObjectOfType<CreateItems>().CreateNewItem(recipe, woods);
            ReloadPanels();

        }
        else
        {
            Debug.Log("nie wystarczajaco itemow");
        }

    }
}
