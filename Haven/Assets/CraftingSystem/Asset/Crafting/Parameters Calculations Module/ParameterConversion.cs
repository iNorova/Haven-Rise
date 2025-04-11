using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterConversion : MonoBehaviour {

    public void AddItemToQueue(Item[] items, Item item)
    {
        item.exp = item.getTimeParameter();
        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(items, item);
    }

    public void CreateNewMaterial(Metal[] chosenMetals, Conditioner[] chosenconditioners, ExerciseMetod exerciseMetod)
    {
        Metal new_metal = new Metal("New Metal");
        new_metal.image = FindObjectOfType<Configuration>().defaultCreatedMetalSprite;

        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenMetals.Length; i++)
        {

            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }

            chosenMetals[i].Multiple(curentExerciseMultiplier);
            new_metal.Add(chosenMetals[i]);


        }

        new_metal.Divide(chosenMetals.Length);

        for (int i = 0; i < chosenconditioners.Length; i++)
        {
            new_metal.MultipleConditioner(chosenconditioners[i]);

        }

        if (chosenconditioners.Length != 0)
        {
            new_metal.color /= chosenconditioners.Length;
        }


        new_metal.usedConditioner = chosenconditioners.Length;

        if (exerciseMetod!= null)
        {
            new_metal.usedConditioner += exerciseMetod.conditionerUsed;
        }

        for (int i = 0; i < chosenMetals.Length; i++)
        {
            AddItemToQueue(chosenMetals, new_metal);
        }

    }

    public void CreateNewMaterial(Wood[] chosenWoods, Conditioner[] chosenConditioner, ExerciseMetod exerciseMetod)
    {
        //Create an instance of new object
        Wood new_base_material = new Wood("New Wood");
        //Add Image from configuration file
        new_base_material.image = FindObjectOfType<Configuration>().defaultCreatedWoodSprite;

        //Calculation of new properties
        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenWoods.Length; i++)
        {
            if (exerciseMetod!= null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenWoods[i].Multiple(curentExerciseMultiplier);
            new_base_material.Add(chosenWoods[i]);


        }

        new_base_material.Divide(chosenWoods.Length);


        for (int i = 0; i < chosenConditioner.Length; i++)
        {
            new_base_material.MultipleConditioner(chosenConditioner[i]);

        }
        if (exerciseMetod != null)
        {
            new_base_material.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_base_material.usedConditioner += chosenConditioner.Length;

        if (chosenConditioner.Length != 0)
        {
            new_base_material.color /= chosenConditioner.Length;
        }

        //Adding right amout of komposit materials to queue 
        if (chosenWoods.Length > 1)
        {
            for (int i = 0; i < chosenWoods.Length - 1; i++)
            {
                AddItemToQueue(chosenWoods, new_base_material);
            }
        }
        else
        {
            AddItemToQueue(chosenWoods, new_base_material);
        }


    }

    public void CreateNewMaterial(Stone[] chosenWoods, Conditioner[] chosenConditioners, ExerciseMetod exerciseMetod)
    {

        Stone new_base_material = new Stone("New Stone");
        new_base_material.image = FindObjectOfType<Configuration>().defaultCreatedWoodSprite;

        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenWoods.Length; i++)
        {
            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenWoods[i].Multiple(curentExerciseMultiplier);
            new_base_material.Add(chosenWoods[i]);


        }

        new_base_material.Divide(chosenWoods.Length);


        for (int i = 0; i < chosenConditioners.Length; i++)
        {
            new_base_material.MultipleConditioner(chosenConditioners[i]);

        }
        if (exerciseMetod != null)
        {
            new_base_material.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_base_material.usedConditioner += chosenConditioners.Length;

        if (chosenConditioners.Length != 0)
        {
            new_base_material.color /= chosenConditioners.Length;
        }

        if (chosenWoods.Length > 1)
        {
            for (int i = 0; i < chosenWoods.Length - 1; i++)
            {
                AddItemToQueue(chosenWoods, new_base_material);
            }
        }
        else
        {
            AddItemToQueue(chosenWoods, new_base_material);
        }


    }

    public void CreateNewMaterial(Leather[] chosenWoods, Conditioner[] chosenConditioners, ExerciseMetod exerciseMetod)
    {

        Leather new_base_material = new Leather("New Leather");
        new_base_material.image = FindObjectOfType<Configuration>().defaultCreatedWoodSprite;

        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenWoods.Length; i++)
        {
            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenWoods[i].Multiple(curentExerciseMultiplier);
            new_base_material.Add(chosenWoods[i]);


        }

        new_base_material.Divide(chosenWoods.Length);


        for (int i = 0; i < chosenConditioners.Length; i++)
        {
            new_base_material.MultipleConditioner(chosenConditioners[i]);

        }
        if (exerciseMetod != null)
        {
            new_base_material.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_base_material.usedConditioner += chosenConditioners.Length;

        if (chosenConditioners.Length != 0)
        {
            new_base_material.color /= chosenConditioners.Length;
        }


        if (chosenWoods.Length > 1)
        {
            for (int i = 0; i < chosenWoods.Length - 1; i++)
            {
                AddItemToQueue(chosenWoods, new_base_material);
            }
        }
        else
        {
            AddItemToQueue(chosenWoods, new_base_material);
        }


    }

    public void CreateNewMaterial(Cloth[] chosenWoods, Conditioner[] chosenConditioners, ExerciseMetod exerciseMetod)
    {
        //Tworzenie instancji nowego obiektu
        Cloth new_base_material = new Cloth("New Cloath");
        //Dodanie do instancji obrazu z pliku konfiguracyjnego
        new_base_material.image = FindObjectOfType<Configuration>().defaultCreatedWoodSprite;

        //Obliczenia związane z uzyskaniem nowych własciwości
        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenWoods.Length; i++)
        {
            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenWoods[i].Multiple(curentExerciseMultiplier);
            new_base_material.Add(chosenWoods[i]);


        }

        new_base_material.Divide(chosenWoods.Length);


        for (int i = 0; i < chosenConditioners.Length; i++)
        {
            new_base_material.MultipleConditioner(chosenConditioners[i]);

        }
        if (exerciseMetod != null)
        {
            new_base_material.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_base_material.usedConditioner += chosenConditioners.Length;

        if (chosenConditioners.Length != 0)
        {
            new_base_material.color /= chosenConditioners.Length;
        }

        if (chosenWoods.Length > 1)
        {
            for (int i = 0; i < chosenWoods.Length - 1; i++)
            {
                AddItemToQueue(chosenWoods, new_base_material);
            }
        }
        else
        {
            AddItemToQueue(chosenWoods, new_base_material);
        }


    }

    public void CreateNewMaterial(Liquid[] chosenWoods, Conditioner[] chosenConditioners, ExerciseMetod exerciseMetod)
    {

        Liquid new_base_material = new Liquid("New Liquid");

        new_base_material.image = FindObjectOfType<Configuration>().defaultCreatedWoodSprite;


        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenWoods.Length; i++)
        {
            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenWoods[i].Multiple(curentExerciseMultiplier);
            new_base_material.Add(chosenWoods[i]);


        }

        new_base_material.Divide(chosenWoods.Length);


        for (int i = 0; i < chosenConditioners.Length; i++)
        {
            new_base_material.MultipleConditioner(chosenConditioners[i]);

        }
        if (exerciseMetod != null)
        {
            new_base_material.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_base_material.usedConditioner += chosenConditioners.Length;

        if (chosenConditioners.Length != 0)
        {
            new_base_material.color /= chosenConditioners.Length;
        }


        if (chosenWoods.Length > 1)
        {
            for (int i = 0; i < chosenWoods.Length - 1; i++)
            {
                AddItemToQueue(chosenWoods, new_base_material);
            }
        }
        else
        {
            AddItemToQueue(chosenWoods, new_base_material);
        }


    }

    public void CreateNewMaterial(Rune[] chosenRunes, Conditioner[] chosenConditioners, ExerciseMetod exerciseMetod)
    {
        Rune new_rune = new Rune("New Rune");
        new_rune.image = FindObjectOfType<Configuration>().defaultCreatedRuneSprite;

        int curentPositionExercise = 0;
        float curentExerciseMultiplier = 1;
        for (int i = 0; i < chosenRunes.Length; i++)
        {
            if (exerciseMetod != null)
            {
                if (curentPositionExercise < exerciseMetod.orderElementsImpact.Length)
                {
                    curentExerciseMultiplier = exerciseMetod.orderElementsImpact[curentPositionExercise];
                }
                else
                {
                    curentExerciseMultiplier = exerciseMetod.otherElementsImpact;
                }
                curentPositionExercise += 1;
            }
            chosenRunes[i].Multiple(curentExerciseMultiplier);
            new_rune.Add(chosenRunes[i]);

        }

        new_rune.Divide( chosenRunes.Length);

        for (int i = 0; i < chosenConditioners.Length; i++)
        {
            new_rune.MultipleConditioner(chosenConditioners[i]);
        }
        if (exerciseMetod != null)
        {
            new_rune.usedConditioner += exerciseMetod.conditionerUsed;
        }
        new_rune.usedConditioner += chosenConditioners.Length;

        if (chosenConditioners.Length != 0)
        {
            new_rune.color /= chosenConditioners.Length;
        }

        if (chosenRunes.Length > 1)
        {
            for (int i = 0; i < chosenRunes.Length - 1; i++)
            {
                AddItemToQueue(chosenRunes, new_rune);
            }
        }
        else
        {
            AddItemToQueue(chosenRunes, new_rune);
        }

    }

    public bool CanCreateMaterial(Item[] chosenWoods, Conditioner[] choosenConditioners)
    {
        if (chosenWoods.Length < 1)
        {
            Debug.Log("Chosen Items to low");
            return false;
        }
        if (chosenWoods.Length == 1 && choosenConditioners.Length == 0)
        {
            Debug.Log("Can't process single item");
            return false;
        }

        float amoutOfSpaceForConditioners = 0;
        float usedConditioners = 0;

        for (int i = 0; i < chosenWoods.Length; i++)
        {
            amoutOfSpaceForConditioners += chosenWoods[i].conditionerCapacity;
            usedConditioners += chosenWoods[i].usedConditioner;
        }
        amoutOfSpaceForConditioners /= chosenWoods.Length;
        usedConditioners /= chosenWoods.Length;

        if (amoutOfSpaceForConditioners < usedConditioners + choosenConditioners.Length)
        {
            Debug.Log("No more space for conditioners");
            return false;
        }

        return true;

    }

}
