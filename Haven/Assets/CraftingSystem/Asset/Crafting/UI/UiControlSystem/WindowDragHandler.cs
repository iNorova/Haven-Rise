using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{

    public static GameObject itemBeginDragged;
    Vector3 startPosition;
    Vector3 dist;
    float posX;
    float posY;


    public void OnBeginDrag(PointerEventData eventData)
    {

        itemBeginDragged = gameObject;
        startPosition = transform.position;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        itemBeginDragged = null;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
}
