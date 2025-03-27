using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item")]
    // data/variables that EVERY item will have
    // adding variables here will add that variable to all scriptable objects
    public string itemName;
    public Sprite itemIcon;
    public bool isStackable;

    // abstract functions - no touchy
    public abstract ItemClass GetItem();
    public abstract MiscClass GetMisc();
    public abstract ConsumableClass GetConsumable();
    public abstract QuestClass GetQuest();

}
