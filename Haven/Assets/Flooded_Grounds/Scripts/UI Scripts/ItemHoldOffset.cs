using UnityEngine;

public class ItemHoldOffset : MonoBehaviour
{
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    public void ApplyOffset(Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = positionOffset;
        transform.localRotation = Quaternion.Euler(rotationOffset);
    }
} 