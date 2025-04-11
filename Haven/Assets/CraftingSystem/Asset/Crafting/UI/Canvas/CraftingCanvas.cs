using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingCanvas : MonoBehaviour {

    [Header("Recipes")]
    public Recipe[] recipe;
    [Header("Creation Metods")]
    public ExerciseMetod[] exerciseMetods;
    [Header("UI elements")]
    public Image image;
    public Text text;
    public GameObject backpackPanel;
    public GameObject chosenPanel;
    public GameObject queuePanel;
    [Header("Prefabs")]
    public GameObject singleItemPrefab;
    public GameObject singleStatusPrefab;
    [Header("Configuration settings")]
    public bool colorsFromConfig = false;
    public bool prefabsFromConfig = true;
    GameObject curentSingleLikeBacpackCanvasBacpack;
    GameObject curentSingleLikeBacpackCanvasChosen;

    public void AddSinglePrefabBackpack(Object obj)
    {
        //Vector3 firstPanelPosition = nextPanelPosition(backpackPanel, 50, quantityBackpack);



        if (singleItemPrefab.GetComponent<LikeBacpackControler>())
        {
            GameObject newSinglePanel;
            if (curentSingleLikeBacpackCanvasBacpack == null || curentSingleLikeBacpackCanvasBacpack.GetComponent<LikeBacpackControler>().isFull())
            {
                curentSingleLikeBacpackCanvasBacpack = Instantiate(singleItemPrefab,
                    new Vector3(0, 0, 0),
                    new Quaternion(0, 0, 0, 0),
                backpackPanel.transform.parent) as GameObject;
                curentSingleLikeBacpackCanvasBacpack.transform.SetParent(backpackPanel.transform, false);
                newSinglePanel = curentSingleLikeBacpackCanvasBacpack.GetComponent<LikeBacpackControler>().getCurentCanvas();
            }
            else
            {
                newSinglePanel = curentSingleLikeBacpackCanvasBacpack.GetComponent<LikeBacpackControler>().getCurentCanvas();
            }
            
            if (newSinglePanel.GetComponentInChildren<Text>())
            {
                newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
            }
            if (newSinglePanel.GetComponent<PanelInfo>().image != null)
            {

                newSinglePanel.GetComponent<PanelInfo>().image.sprite = ((Item)obj).image;
                newSinglePanel.GetComponent<PanelInfo>().image.GetComponent<Image>().color = ((Item)obj).color;


            }
            newSinglePanel.GetComponent<PanelInfo>().item = obj;
            newSinglePanel.GetComponent<PanelInfo>().currentPanel = "plecak";
        }
        else
        {
            GameObject newSinglePanel = Instantiate(singleItemPrefab,
                new Vector3(0, 0, 0),
                new Quaternion(0, 0, 0, 0),
            backpackPanel.transform.parent) as GameObject;

            //newSinglePanel.transform.position = firstPanelPosition;
            newSinglePanel.transform.SetParent(backpackPanel.transform, false);
            newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
            if (newSinglePanel.GetComponent<PanelInfo>().image != null)
            {
                newSinglePanel.GetComponent<PanelInfo>().image.sprite = ((Item)obj).image;
                newSinglePanel.GetComponent<PanelInfo>().image.GetComponent<Image>().color = ((Item)obj).color;
            }
            newSinglePanel.GetComponent<PanelInfo>().item = obj;
            newSinglePanel.GetComponent<PanelInfo>().currentPanel = "plecak";
        }
    }

    public void AddSinglePrefabChosen(Object obj)
    {
        //Vector3 firstPanelPosition = nextPanelPosition(chosenPanel, 50, quantityChosen);
        if (singleItemPrefab.GetComponent<LikeBacpackControler>())
        {
            GameObject newSinglePanel;
            if (curentSingleLikeBacpackCanvasChosen == null || curentSingleLikeBacpackCanvasChosen.GetComponent<LikeBacpackControler>().isFull())
            {
                curentSingleLikeBacpackCanvasChosen = Instantiate(singleItemPrefab,
                    new Vector3(0, 0, 0),
                    new Quaternion(0, 0, 0, 0),
                backpackPanel.transform.parent) as GameObject;
                curentSingleLikeBacpackCanvasChosen.transform.SetParent(chosenPanel.transform, false);
                newSinglePanel = curentSingleLikeBacpackCanvasChosen.GetComponent<LikeBacpackControler>().getCurentCanvas();
            }
            else
            {
                newSinglePanel = curentSingleLikeBacpackCanvasChosen.GetComponent<LikeBacpackControler>().getCurentCanvas();
            }

            if (newSinglePanel.GetComponentInChildren<Text>())
            {
                newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
            }
            if (newSinglePanel.GetComponent<PanelInfo>().image != null)
            {
                newSinglePanel.GetComponent<PanelInfo>().image.sprite = ((Item)obj).image;
                newSinglePanel.GetComponent<PanelInfo>().image.GetComponent<Image>().color = ((Item)obj).color;
            }
            newSinglePanel.GetComponent<PanelInfo>().item = obj;
            newSinglePanel.GetComponent<PanelInfo>().currentPanel = "wybrane";
        }
        else
        {
            GameObject newSinglePanel = Instantiate(singleItemPrefab,
            new Vector3(0, 0, 0),
            new Quaternion(0, 0, 0, 0),
            chosenPanel.transform.parent) as GameObject;

            //newSinglePanel.transform.position = firstPanelPosition;
            newSinglePanel.transform.SetParent(chosenPanel.transform, false);
            newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
            newSinglePanel.GetComponent<PanelInfo>().item = obj;
            if (newSinglePanel.GetComponent<PanelInfo>().image != null)
            {
                newSinglePanel.GetComponent<PanelInfo>().image.sprite = ((Item)obj).image;
                newSinglePanel.GetComponent<PanelInfo>().image.GetComponent<Image>().color = ((Item)obj).color;
            }
            newSinglePanel.GetComponent<PanelInfo>().currentPanel = "wybrane";
        }

    }

    public void AddSinglePrefabQueue(Ready obj)
    {
        //Vector3 firstPanelPosition = nextPanelPosition(queuePanel, 50, quantityQueue);

        GameObject newSinglePanel = Instantiate(singleStatusPrefab,
            new Vector3(0, 0, 0),
            new Quaternion(0, 0, 0, 0),
            queuePanel.transform.parent) as GameObject;

        //newSinglePanel.transform.position = firstPanelPosition;
        newSinglePanel.transform.SetParent(queuePanel.transform, false);
        newSinglePanel.GetComponent<PanelInfo>().item = obj.item;
        newSinglePanel.GetComponent<PanelInfo>().currentPanel = "aktualizowane";
        newSinglePanel.GetComponentInChildren<Text>().text = obj.item.name;
        obj.slider = newSinglePanel.GetComponent<PanelInfo>().slider;
        obj.progressText = newSinglePanel.GetComponent<PanelInfo>().progressText;
        obj.slider.value = obj.progress;
        obj.progressText.text = ((int)(obj.executionTime - obj.progress * obj.executionTime)).ToString();

        if (obj.ready)
        {
            newSinglePanel.GetComponent<PanelInfo>().currentPanel = "gotowe";
        }

    }

    public void ReloadPanels()
    {
        if (FindObjectOfType<UiManagment>().curentCanvas != null && FindObjectOfType<UiManagment>().curentCanvas.isActiveAndEnabled){
            UiManagment uiMenagment = FindObjectOfType<UiManagment>();

            curentSingleLikeBacpackCanvasBacpack = null;
            curentSingleLikeBacpackCanvasChosen = null;

            PanelInfo[] itemsinfo = FindObjectsOfType<PanelInfo>();
            for (int i = 0; i < itemsinfo.Length; i++)
            {
                Destroy(itemsinfo[i].gameObject);
            }

            uiMenagment.quantityBackpack = 0;
            for (int i = 0; i < uiMenagment.backpack.Count; i++)
            {
                if (uiMenagment.backpack[i] != null)
                {
                    AddSinglePrefabBackpack(uiMenagment.backpack[i]);
                    uiMenagment.quantityBackpack += 1;
                }
            }

            uiMenagment.quantityChosen = 0;
            for (int i = 0; i < uiMenagment.chosenItems.Count; i++)
            {
                if (uiMenagment.chosenItems[i] != null)
                {
                    AddSinglePrefabChosen(uiMenagment.chosenItems[i]);
                    uiMenagment.quantityChosen += 1;
                }
            }

            uiMenagment.quantityQueue = 0;
            for (int i = 0; i < uiMenagment.preparationQueue.Length; i++)
            {
                if (uiMenagment.preparationQueue[i] != null)
                {
                    AddSinglePrefabQueue(uiMenagment.preparationQueue[i]);
                    uiMenagment.quantityQueue += 1;
                }
            }
        }
    }

    private void OnEnable()
    {
        setCanvasColors();
        FindObjectOfType<UiManagment>().curentCanvas = this.GetComponent<CraftingCanvas>();
        if (prefabsFromConfig)
        {
            singleItemPrefab = FindObjectOfType<Configuration>().ItemPrefab;
            singleStatusPrefab = FindObjectOfType<Configuration>().ProgressbarPrefab;
        }
        ReloadPanels();
    }

    public void setCanvasColors()
    {
        if (colorsFromConfig)
        {
            this.GetComponent<Image>().color = FindObjectOfType<Configuration>().backgroundColor;
            backpackPanel.GetComponentsInParent<Image>()[2].color = FindObjectOfType<Configuration>().panelsColor;
            chosenPanel.GetComponentsInParent<Image>()[2].color = FindObjectOfType<Configuration>().panelsColor;
            queuePanel.GetComponentsInParent<Image>()[2].color = FindObjectOfType<Configuration>().panelsColor;
            this.GetComponentsInChildren<Button>()[1].colors = FindObjectOfType<Configuration>().ButtonColors;
        }
    }

    public void cleanChosenItems(Object[] otherItems)
    {
        FindObjectOfType<UiManagment>().chosenItems.Clear();
        for (int i = 0; i < otherItems.Length; i++)
        {
            FindObjectOfType<UiManagment>().chosenItems.Add(otherItems[i]);
        }
        FindObjectOfType<UiManagment>().ReturnAllItemToBackpack();
    }

    public void DragItemToChosen()
    {
        FindObjectOfType<UiManagment>().dragItemToChosen();
    }

    public void CloseCanvas()
    {
        FindObjectOfType<UiManagment>().closeCurentCanvas();
    }

    public void ChoseRecipe()
    {
        FindObjectOfType<UiManagment>().ChoseRecipe();
    }

    public int[] checkItemsOfTypeInChosenItems(System.Type[] itemsTypes)
    {
        int[] types = new int[itemsTypes.Length + 1];

        for (int i = 0; i < FindObjectOfType<UiManagment>().chosenItems.Count; i++)
        {

            if (FindObjectOfType<UiManagment>().chosenItems[i] != null)
            {
                bool add = false;
                for (int j = 0; j < itemsTypes.Length; j++)
                {
                    if (FindObjectOfType<UiManagment>().chosenItems[i].GetType().Equals(itemsTypes[j]))
                    {
                        types[j] += 1;
                        add = true;
                    }
                }
                if (!add)
                {
                    types[types.Length - 1] += 1;
                }

            }
        }
        return types;
    }
}
