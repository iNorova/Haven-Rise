using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIKeyboardControler : MonoBehaviour {


	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FindObjectOfType<UiManagment>().closeCurentCanvas();
        }
	}
}
