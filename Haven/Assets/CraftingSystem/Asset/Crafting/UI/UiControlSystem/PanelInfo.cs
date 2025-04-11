using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelInfo : MonoBehaviour {

    public Object item;
    public string currentPanel;
    public Slider slider;
    public Image image;
    public Text progressText;
    public Canvas mouseShow;
    GameObject createToShow;

    public void onClick()
    {
        if (currentPanel == "plecak")
        {
            FindObjectOfType<UiManagment>().selectedItem = item;
        }
        else if(currentPanel == "wybrane")
        {
            FindObjectOfType<UiManagment>().selectedItem = item;
            FindObjectOfType<UiManagment>().returnItemToBackpack();
        }
        else if (currentPanel == "gotowe")
        {
            FindObjectOfType<UiManagment>().selectedItem = item;
            FindObjectOfType<UiManagment>().pickItemFromQueue();
        }
    }

    public void onClickRecipe()
    {
        FindObjectOfType<UiManagment>().updateImage(((Recipe)item).image);
        FindObjectOfType<UiManagment>().chosenRecipe = (Recipe)item;
        FindObjectOfType<UiManagment>().chosenRecipeCanvas.gameObject.SetActive(false);
    }

   public void dragg()
    {
        createToShow = Instantiate(mouseShow.gameObject, transform.position, transform.rotation);
        createToShow.GetComponentInChildren<Mouse>().updateText(item.ToString());
        Vector3 newVector = Input.mousePosition + new Vector3(0.6f * createToShow.GetComponentInChildren<Mouse>().gameObject.GetComponent<RectTransform>().rect.width,
            -0.5f * createToShow.GetComponentInChildren<Mouse>().gameObject.GetComponent<RectTransform>().rect.height, 0);

        Vector3 newCam = FindObjectOfType<Camera>().WorldToViewportPoint(newVector);

        if (newCam.y < 10)
        {
            newVector.y += 120;
        }

        createToShow.GetComponentInChildren<Mouse>().gameObject.transform.position = newVector;


    }

    public void onClickExerciseMetod()
    {
        FindObjectOfType<UiManagment>().updateImage(((ExerciseMetod)item).name);
        FindObjectOfType<UiManagment>().exerciseMetod = (ExerciseMetod)item;
        FindObjectOfType<UiManagment>().chosenExerciseMetodCanvas.gameObject.SetActive(false);
    }


    public void draggExit()
    {
        Destroy(createToShow);
    }

    public void Update()
    {
        if (createToShow != null)
        {
            Vector3 newVector = Input.mousePosition + new Vector3(0.6f * createToShow.GetComponentInChildren<Mouse>().gameObject.GetComponent<RectTransform>().rect.width,
            -0.5f * createToShow.GetComponentInChildren<Mouse>().gameObject.GetComponent<RectTransform>().rect.height, 0);

            Vector3 newCam = FindObjectOfType<Camera>().WorldToViewportPoint(newVector);

            if (newCam.y < 10)
            {
                newVector.y += 120;
            }

            createToShow.GetComponentInChildren<Mouse>().gameObject.transform.position = newVector;
        }
    }

    public void OnDestroy()
    {
        Destroy(createToShow);
    }
    private void OnDisable()
    {
        Destroy(createToShow);
    }

}
