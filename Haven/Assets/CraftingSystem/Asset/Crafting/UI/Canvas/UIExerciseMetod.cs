using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIExerciseMetod : MonoBehaviour
{

    public GameObject singleItemPrefab;
    public GameObject metodCreationPanel;
    public bool colorsFromConfig = false;

    // Use this for initialization
    void Start()
    {
        ReloadExerciseMetod();
    }

    public void ReloadExerciseMetod()
    {
        PanelInfo[] itemsinfo = FindObjectsOfType<PanelInfo>();
        for (int i = 0; i < itemsinfo.Length; i++)
        {
            Destroy(itemsinfo[i].gameObject);
        }


        for (int i = 0; i < FindObjectOfType<UiManagment>().curentCanvas.exerciseMetods.Length; i++)
        {
            if (FindObjectOfType<UiManagment>().curentCanvas.exerciseMetods[i] != null)
            {
                addSinglePrefabExerciseMetod(FindObjectOfType<UiManagment>().curentCanvas.exerciseMetods[i]);

            }
        }
    }

    public void addSinglePrefabExerciseMetod(Object obj)
    {

        GameObject newSinglePanel = Instantiate(singleItemPrefab,
            new Vector3(0, 0, 0),
            new Quaternion(0, 0, 0, 0),
            metodCreationPanel.transform.parent) as GameObject;

        newSinglePanel.transform.SetParent(metodCreationPanel.transform, false);
        newSinglePanel.GetComponentInChildren<Text>().text = obj.name;
        newSinglePanel.GetComponent<PanelInfo>().item = obj;
        newSinglePanel.GetComponent<PanelInfo>().currentPanel = "wybrane";
    }

    public void OnEnable()
    {
        setCanvasColors();
        ReloadExerciseMetod();
    }

    public void back()
    {
        this.gameObject.SetActive(false);
        FindObjectOfType<UiManagment>().curentCanvas.gameObject.SetActive(true);
    }

    public void setCanvasColors()
    {
        if (colorsFromConfig)
        {
            this.GetComponent<Image>().color = FindObjectOfType<Configuration>().backgroundColorMetods;
            metodCreationPanel.GetComponentsInParent<Image>()[1].color = FindObjectOfType<Configuration>().panelColorMetods;
        }
    }
}
