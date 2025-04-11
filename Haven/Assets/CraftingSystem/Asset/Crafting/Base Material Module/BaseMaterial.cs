using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface BaseMaterial<T> {

    /***
     * Adding base material to created one
     ***/
    void Add(T materialBazowy);

    /***
     * Use of crafting metod
     ***/
    void Multiple(float multiplier);

    /***
     * divide by used amount of items
     ***/
    void Divide(int i);

    /***
    * Use of conditioners
    ***/
    void MultipleConditioner(Conditioner uzdatniacz);
}
