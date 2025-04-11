using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Configuration : MonoBehaviour {
    /*Singleton*/
    private static Configuration m_oInstance = null;
    private int m_nCounter = 0;
    private static readonly object m_oPadLock = new object();

    public static Configuration Instance
    {
        get
        {
            lock (m_oPadLock)
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new Configuration();
                }
                return m_oInstance;
            }

        }
    }

    private Configuration()
    {
        m_nCounter = 1;
    }
    /*Koniec Singletona*/

    [Header("Defaults Sprites")]
    public Sprite defaultSprite;
    [Space]
    public Sprite defaultCreatedMetalSprite;

    public Sprite defaultCreatedWoodSprite;

    public Sprite defaultCreatedRuneSprite;

    [Header("Prefabs")]
    public GameObject ItemPrefab;

    public GameObject ProgressbarPrefab;

    [Header("UI Design")]
    public Color32 backgroundColor;

    public Color32 panelsColor;
    [Space][Header("Button Color")]
    public ColorBlock ButtonColors;

    [Header("UI recipe design.")]
    public Color32 backgroundColorRecipe;
    public Color32 panelColorRecipe;

    [Header("UI Creation Metods Design")]
    public Color32 backgroundColorMetods;
    public Color32 panelColorMetods;
}
