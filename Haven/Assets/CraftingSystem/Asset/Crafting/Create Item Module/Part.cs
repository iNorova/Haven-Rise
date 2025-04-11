using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "nowyPrzedmiot", menuName = "Item/Part")]
public class Part : Item {

    public Part(string name)
    {
        this.name = name;
    }

    public Part()
    {

    }

}
