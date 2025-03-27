using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new Quest Item", menuName = "Item/Quest Item")]
public class QuestClass : ItemClass
{
    [Header("Quest")]
    // variables specific to quest objective items
    public TotemType totemType;
    public enum TotemType
    { Green, Blue, Purple, Pink, Red }
    public override ItemClass GetItem() { return this; }
    public override QuestClass GetQuest() { return this; }
    public override MiscClass GetMisc() { return null; }
    public override ConsumableClass GetConsumable() { return null; }
    
}
