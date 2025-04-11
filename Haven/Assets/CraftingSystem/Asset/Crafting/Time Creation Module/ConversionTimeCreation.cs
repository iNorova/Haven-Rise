using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversionTimeCreation : MonoBehaviour {

    /*Singleton*/
    private static ConversionTimeCreation m_oInstance = null;
    private int m_nCounter = 0;
    private static readonly object m_oPadLock = new object();

    public static ConversionTimeCreation Instance
    {
        get
        {
            lock (m_oPadLock)
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new ConversionTimeCreation();
                }
                return m_oInstance;
            }

        }
    }

    private ConversionTimeCreation()
    {
        m_nCounter = 1;
    }
    /*Koniec Singletona*/

    IEnumerator WaitUntilDone(Ready gotowe)
    {
        gotowe.ready = false;
        float startTime = Time.time;
        while (startTime + gotowe.executionTime > Time.time)
        {
            yield return new WaitForSeconds(0.1f);
            gotowe.progress = (Time.time - startTime) / gotowe.executionTime;
            FindObjectOfType<UiManagment>().UpdateProgressInQueue(gotowe);
        }
        gotowe.ready = true;
        UpdateItemStatus(gotowe.item); 
    }

    public float TimeConversion(Item[] metals)
    {
        float time = 0;
        for (int i = 0; i < metals.Length; i++)
        {
            time += metals[i].getTimeParameter();
        }

        time /= metals.Length;

        return time;

    }

    public void AddItemToQueue(Item[] metals,Object item)
    { 
        Ready gotowe = new Ready();
        gotowe.executionTime = TimeConversion(metals);
        gotowe.item = item;

        Ready[] itemsInProgress = FindObjectOfType<UiManagment>().preparationQueue;
        for (int i = 0; i < itemsInProgress.Length; i++)
        {
            if (itemsInProgress[i] == null)
            {
                itemsInProgress[i] = gotowe;
                FindObjectOfType<UiManagment>().preparationQueue = itemsInProgress;
                FindObjectOfType<UiManagment>().curentCanvas.ReloadPanels();
                break;
            }
        }
        StartCoroutine(WaitUntilDone(gotowe));
     
    }

    public void AddItemToQueue(float time,Object item)
    {
        Ready gotowe = new Ready();
        gotowe.executionTime = time;
        gotowe.item = item;

        Ready[] itemsInProgress = FindObjectOfType<UiManagment>().preparationQueue;
        for (int i = 0; i < itemsInProgress.Length; i++)
        {
            if (itemsInProgress[i] == null)
            {
                itemsInProgress[i] = gotowe;
                FindObjectOfType<UiManagment>().preparationQueue = itemsInProgress;
                FindObjectOfType<UiManagment>().curentCanvas.ReloadPanels();
                break;
            }
        }
        StartCoroutine(WaitUntilDone(gotowe));
    }

    public void UpdateItemStatus(Object przedmiot)
    {
        FindObjectOfType<UiManagment>().updateItemInQueue(przedmiot);
    }
}
