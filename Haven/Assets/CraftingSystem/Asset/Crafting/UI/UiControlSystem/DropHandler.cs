using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropHandler : MonoBehaviour, IDropHandler {


    public void OnDrop(PointerEventData eventData)
    {
        FindObjectOfType<UiManagment>().dragItemToChosen();
    }

}
