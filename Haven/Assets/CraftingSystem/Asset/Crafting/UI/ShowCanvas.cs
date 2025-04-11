using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowCanvas : MonoBehaviour, IPointerClickHandler {

    public Canvas canvasToShow;

    public void OnPointerClick(PointerEventData eventData)
    {
        FindObjectOfType<UiManagment>().closeCurentCanvas();
        canvasToShow.gameObject.SetActive(true);
    }


}
