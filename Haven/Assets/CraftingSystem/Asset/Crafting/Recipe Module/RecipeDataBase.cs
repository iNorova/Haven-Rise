using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeDataBase : MonoBehaviour {

    /*Singleton*/
    private static RecipeDataBase m_oInstance = null;
    private int m_nCounter = 0;
    private static readonly object m_oPadLock = new object();

    public static RecipeDataBase Instance
    {
        get
        {
            lock (m_oPadLock)
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new RecipeDataBase();
                }
                return m_oInstance;
            }

        }
    }

    private RecipeDataBase()
    {
        m_nCounter = 1;
    }
    /*Koniec Singletona*/

    public Recipe[] metalRecipes;

    public Recipe[] woodRecipes;

}
