using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LikeBacpackControler : MonoBehaviour {

    public GameObject[] CanvasList;

    public bool isFull() {
        if (position<CanvasList.Length)
        {
            return false;
        }
        return true;
    }

    public int position=0;

    public GameObject getCurentCanvas()
    {
        position += 1;
        CanvasList[position - 1].SetActive(true);
        return CanvasList[position-1];
    }

    private void OnEnable()
    {
        foreach (GameObject i in CanvasList)
        {
            i.SetActive(false);
        }
    }
}
