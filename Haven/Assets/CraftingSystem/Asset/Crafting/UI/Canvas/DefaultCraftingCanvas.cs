using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultCraftingCanvas : CraftingCanvas
{
    // Use this for initialization
    void Start()
    {
        ReloadPanels();
    }

    public void Create()
    {
        Recipe recipe = FindObjectOfType<UiManagment>().chosenRecipe;

        if (recipe != null && FindObjectOfType<CreateItems>().recipeItemsIn(recipe, FindObjectOfType<UiManagment>().chosenItems))
        {

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

            FindObjectOfType<CreateItems>().CreateNewItem(recipe);
            ReloadPanels();

        }
        else
        {
            Debug.Log("nie wystarczajaco itemow");
        }

    }


}
