using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIrecipes : MonoBehaviour {


    public GameObject singleItemPrefab;
    public GameObject recipePanel;
    public bool colorsFromConfig = false;

    // Use this for initialization
    void Start () {
        ReloadRecipes();
    }

    public void ReloadRecipes()
    {
        PanelInfo[] itemsinfo = FindObjectsOfType<PanelInfo>();
        for (int i = 0; i < itemsinfo.Length; i++)
        {
            Destroy(itemsinfo[i].gameObject);
        }


        for (int i = 0; i < FindObjectOfType<UiManagment>().curentCanvas.recipe.Length; i++)
        {
            if (FindObjectOfType<UiManagment>().curentCanvas.recipe[i] != null)
            {
                addSinglePrefabRecipe(FindObjectOfType<UiManagment>().curentCanvas.recipe[i]);

            }
        }
    }

    public void addSinglePrefabRecipe(Object obj)
    {

        GameObject newSinglePanel = Instantiate(singleItemPrefab,
            new Vector3(0, 0, 0),
            new Quaternion(0, 0, 0, 0),
            recipePanel.transform.parent) as GameObject;

        //newSinglePanel.transform.position = firstPanelPosition;
        newSinglePanel.transform.SetParent(recipePanel.transform, false);
        newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
        newSinglePanel.GetComponent<PanelInfo>().item = obj;
        newSinglePanel.GetComponent<PanelInfo>().currentPanel = "wybrane";
    }

    public void OnEnable()
    {
        setCanvasColors();
        ReloadRecipes();
    }

    public void setCanvasColors()
    {
        if (colorsFromConfig)
        {
            this.GetComponent<Image>().color = FindObjectOfType<Configuration>().backgroundColorRecipe;
            recipePanel.GetComponentsInParent<Image>()[1].color = FindObjectOfType<Configuration>().panelColorRecipe;
        }
    }

    public void back()
    {
        this.gameObject.SetActive(false);
        FindObjectOfType<UiManagment>().curentCanvas.gameObject.SetActive(true);
    }


}
