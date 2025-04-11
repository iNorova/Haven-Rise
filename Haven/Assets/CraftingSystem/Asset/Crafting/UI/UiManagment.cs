using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiManagment : MonoBehaviour {

    /*Singleton*/
    private static UiManagment m_oInstance = null;
    private int m_nCounter = 0;
    private static readonly object m_oPadLock = new object();

    public static UiManagment Instance
    {
        get
        {
            lock (m_oPadLock)
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new UiManagment();
                }
                return m_oInstance;
            }
 
        }
    }

    private UiManagment()
    {
        m_nCounter = 1;
    }
    /*Koniec Singletona*/

    public Canvas mainCanvas;

    public Canvas MeltCanvas;
    public GameObject singleItemPrefab;
    public GameObject singleStatusPrefab;
    public Object selectedItem;
    public Ready[] preparationQueue = new Ready[20];
    public CraftingCanvas curentCanvas;
    public int quantityBackpack = 0;
    public int quantityChosen = 0;
    public int quantityQueue = 0;

    //public Object[] chosenItems = new Object[20];
    public List<Object> backpack = new List<Object>();
    public List<Object> chosenItems = new List<Object>();

    public Canvas chosenRecipeCanvas;
    public Recipe chosenRecipe;
    public Canvas chosenExerciseMetodCanvas;
    public ExerciseMetod exerciseMetod;

    public void dragItemToChosen()
    {
        if (selectedItem != null)
        {
            int item_index = backpack.IndexOf(selectedItem);
            chosenItems.Add(selectedItem);
            backpack.RemoveAt(item_index);
            selectedItem = null;
            curentCanvas.ReloadPanels();
        }
    }

    public void returnItemToBackpack()
    {
        if (selectedItem != null)
        {
            int item_index = chosenItems.IndexOf(selectedItem);
            backpack.Add(selectedItem);
            chosenItems.RemoveAt(item_index);
            selectedItem = null;
            curentCanvas.ReloadPanels();
        }

    }

    public void pickItemFromQueue()
    {

        for (int i = 0; i < preparationQueue.Length; i++)
        {
            if (preparationQueue[i] != null && preparationQueue[i].item == selectedItem)
            {
                preparationQueue[i] = null;
                break;
            }
        }

        backpack.Add(selectedItem);
        FindObjectOfType<Backpack>().addCraftingExp(((Item)selectedItem).exp);
        selectedItem = null;
        curentCanvas.ReloadPanels();
    }

    public void ReturnAllItemToBackpack()
    {
        foreach (Object obj in chosenItems)
        {
            if (chosenItems != null)
            {
                backpack.Add(obj);
            }
        }
        chosenItems.Clear();
        curentCanvas.ReloadPanels();
    }

    public void updateItemInQueue(Object item)
    {
        for (int i = 0; i < preparationQueue.Length;i++)
        {
            if (preparationQueue[i] != null && preparationQueue[i].item == item)
            {
                preparationQueue[i].ready = true;
                preparationQueue[i].slider.value = 1;
                break;
            }
        }
        curentCanvas.ReloadPanels();
    }

    public void UpdateProgressInQueue(Ready ready)
    {
        for (int i = 0; i < preparationQueue.Length; i++)
        {
            if (preparationQueue[i] != null && preparationQueue[i].item == ready.item)
            {
                preparationQueue[i].progress = ready.progress;
                preparationQueue[i].slider.value = ready.progress;
                preparationQueue[i].progressText.text = ((int)(ready.executionTime - ready.progress * ready.executionTime)).ToString();
            }
        }
    }

    public bool havePlace(List<Object> container)
    {
        for (int i = 0; i < container.Count; i++)
        {
            if (container[i] == null)
            {
                return true;
            }
        }
        return false;
    }

    public void Start()
    {
        backpack = FindObjectOfType<Backpack>().items;
    }

    public void ChoseRecipe()
    {
        curentCanvas.gameObject.SetActive(false);
        chosenRecipeCanvas.gameObject.SetActive(true);
    }

    public void ChoseExerciseMEtod()
    {
        curentCanvas.gameObject.SetActive(false);
        chosenExerciseMetodCanvas.gameObject.SetActive(true);
    }

    public void updateImage(Sprite sprite)
    {
        curentCanvas.gameObject.SetActive(true);
        curentCanvas.image.sprite = sprite;
    }

    public void updateImage(string name)
    {
        curentCanvas.gameObject.SetActive(true);
        curentCanvas.text.text = name;
    }

    public void closeCurentCanvas()
    {
        curentCanvas.gameObject.SetActive(false);
    }

 

}
