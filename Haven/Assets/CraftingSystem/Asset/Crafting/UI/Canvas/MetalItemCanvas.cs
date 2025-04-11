using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalItemCanvas : CraftingCanvas {

    

    // Use this for initialization
    void Start () {
        ReloadPanels();
	}

    public void Create()
    {
        int quantityMetals = 0;
        Recipe recipe = FindObjectOfType<UiManagment>().chosenRecipe;
        
        for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
        {
            if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Metal)))
            {
                quantityMetals += 1;
            }
        }

        if (recipe != null && FindObjectOfType<CreateItems>().checkRecipe(recipe, FindObjectOfType<UiManagment>().chosenItems, typeof(Metal)))
        {

            int met = 0;
            Metal[] metals = new Metal[quantityMetals];
            for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
            {
                if (FindObjectOfType<UiManagment>().chosenItems[i] != null && FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(typeof(Metal)))
                {
                    metals[met] = (Metal)FindObjectOfType<UiManagment>().chosenItems[i];
                    FindObjectOfType<UiManagment>().chosenItems[i] = null;
                    met += 1;
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

            FindObjectOfType<CreateItems>().CreateNewItem(recipe, metals);
            ReloadPanels();

        }
        else
        {
            Debug.Log("nie wystarczajaco itemow");
        }

    }

}
