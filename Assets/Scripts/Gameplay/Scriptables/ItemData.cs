using UnityEngine;


public enum ItemType
{
    Syringe,
    Collectable,
    Obstacle
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public ItemType itemType;
    public float itemEffectValue;
}
