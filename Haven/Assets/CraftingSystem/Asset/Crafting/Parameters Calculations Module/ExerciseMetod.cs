using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewExerciseMetod", menuName = "ExerciseMetod", order = 1)]
public class ExerciseMetod : ScriptableObject {

    public float[] orderElementsImpact;
    public float otherElementsImpact;
    public string description;
    public string name;
    public int conditionerUsed = 0;

    public override string ToString()
    {
        return name + "\n" + description; 
    }
}
