using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public static GameObject itemBeginDragged;
    Vector3 startPosition;
    Transform startParent;


    public void OnBeginDrag(PointerEventData eventData)
    {
        itemBeginDragged = gameObject;
        startPosition = transform.position;
        startParent = transform.parent;
        transform.parent = FindObjectOfType<UiManagment>().mainCanvas.transform;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        GetComponent<PanelInfo>().onClick();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        itemBeginDragged = null;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        if (transform.parent != startParent)
        {
            transform.position = startPosition;
            transform.parent = startParent;
        }
    }

}
