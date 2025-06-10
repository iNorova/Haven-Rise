using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Loop_Background : MonoBehaviour
{
    [SerializeField] private RawImage image;
    [SerializeField] private float xValue;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        image.uvRect = new Rect(image.uvRect.position + new Vector2(xValue, 0) * Time.deltaTime, image.uvRect.size);
    }
}
