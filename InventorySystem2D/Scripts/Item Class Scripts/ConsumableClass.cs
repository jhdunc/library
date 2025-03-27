using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new Quest Item", menuName = "Item/Consumable")]
public class ConsumableClass : ItemClass
{
    [Header("Consumable")]
    // variables specific to consumables
    // for this example, I have given the option to choose
    // an attunement to a totem that matches the Quest class variables.
    public TotemAttune totemAttune;
    public enum TotemAttune
    { Green, Blue, Purple, Pink, Red }

    public override ItemClass GetItem() { return this; }
    public override QuestClass GetQuest() { return null; }
    public override MiscClass GetMisc() { return null; }
    public override ConsumableClass GetConsumable() { return this; }
}
