using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Items can be moved with left mouse button to another slot")]
    public bool canMoveItems = true;
    [Tooltip("Right-clicking a stack will split the stack. Only works if Can Move Items is enabled.")]
    public bool canSplitStacks = true;


    // these variables are Serialized Fields
    // this means they can be private since they are not accessed by other scripts
    // but still be visible in the inspector
    [Header("Inventory Setup")]
    [SerializeField] private GameObject slotHolder;

    // array for starting items (for player or for testing)
    [Tooltip("You can add items here to spawn in the inventory for testing")]
    [SerializeField] private SlotClass[] startingItems;
    [SerializeField] private GameObject itemCursor; // for Moving Items

    private SlotClass[] items;
    private GameObject[] slots;

    #region Moving Items Variables

    private SlotClass movingSlot;
    private SlotClass tempSlot;
    private SlotClass originSlot;
    bool isMovingItem;
    #endregion Moving Items Variables

    private void Start()
    {
        slots = new GameObject[slotHolder.transform.childCount];
        items = new SlotClass[slots.Length];



        // setup starting items
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new SlotClass();
        }

        // initialize all of the slots
        for (int i = 0; i < startingItems.Length; i++)
        {
            items[i] = startingItems[i];
        }

        // set all the slots
        for (int i = 0; i < slotHolder.transform.childCount; i++)
            slots[i] = slotHolder.transform.GetChild(i).gameObject;

        RefreshUI();

    }
    private void Update()
    {
        itemCursor.SetActive(isMovingItem);
        itemCursor.transform.position = Input.mousePosition;
        if (isMovingItem)
        {
            itemCursor.GetComponent<Image>().sprite = movingSlot.GetItem().itemIcon;
        }
        if (canMoveItems)
        {
            if (Input.GetMouseButtonDown(0)) // LMB click!
            {
                if (isMovingItem)
                {
                    EndItemMove();
                }

                else
                {
                    BeginItemMove();
                }
            }
            else if (Input.GetMouseButtonDown(1) && canSplitStacks) // RMB click!
            {
                if (isMovingItem)
                {
                    EndItemMove_Single();
                }

                else
                {
                    BeginItemMove_Half();
                }
            }
        }

    }

    #region Inventory Utility
    public void RefreshUI()
    {
        //check items in inventory and update the on-screen inventory to match
        for (int i = 0; i < slots.Length; i++)
        {
            try
            {
                // if there is an item stored in the item slot, enable the image and set it to the correct
                // item icon from the scriptable object.
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = items[i].GetItem().itemIcon;

                // if the item is a stackable item (a toggle in the scriptable object), then
                // show quantity of item
                if (items[i].GetItem().isStackable)
                {
                    slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().enabled = true;
                    slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = items[i].GetQuantity() + "";
                }
                // otherwise do not show quantity
                else { slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().enabled = false; }
            }
            catch
            {
                // if the above gives an error (no item stored in the slot), then do not show
                // a sprite, and disable the image component
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = null;
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
                slots[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().enabled = false;
            }
        }
    }
    public bool Add(ItemClass item, int quantity)
    {
        SlotClass slot = Contains(item);

        //check if inventory contains item and is stackable
        if (slot != null && slot.GetItem().isStackable)
        {
            slot.AddQuantity(1);
        }
        else
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].GetItem() == null) // this is an empty slot
                {
                    items[i].AddItem(item, quantity);
                    break;
                }
            }
        }
        RefreshUI();
        return true;
    }

    public bool Remove(ItemClass item)
    {
        //check if item exists in the inventory
        SlotClass temp = Contains(item);
        if (temp != null)
        {
            // if more than one item is in the item slot (stackable), remove one but leave the rest
            if (temp.GetQuantity() > 1)
                temp.SubQuantity(1);
            else
            {
                // if item is not stackable, and item exists, remove the item and slot.
                int subSlotIndex = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].GetItem() == item)
                    {
                        subSlotIndex = i;
                        break;
                    }
                }
                items[subSlotIndex].Clear();
            }
        }
        else
        {
            // if item does not exist
            return false;
        }

        RefreshUI();
        return true;
    }
    public SlotClass Contains(ItemClass item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].GetItem() == item)
            { return items[i]; }
        }
        return null;
    }
    #endregion Inventory Utility

    #region Moving Items
    private bool BeginItemMove_Half() // Stack Splitting
    {
        originSlot = GetClosestSlot();
        if (originSlot == null || originSlot.GetItem() == null)
        {
            return false; // there is no item to split
        }
        // Currently picks up half the stack rounded up.
        // if rounding down is preferred,
        // change instances of CeilToInt to FloorToInt in the next two lines
        movingSlot = new SlotClass(originSlot.GetItem(), Mathf.CeilToInt(originSlot.GetQuantity() / 2f));
        originSlot.SubQuantity(Mathf.CeilToInt(originSlot.GetQuantity() / 2f));
        if (originSlot.GetQuantity() == 0)
            originSlot.Clear();

        isMovingItem = true;
        RefreshUI();
        return true;
    }

    private bool BeginItemMove()
    {
        originSlot = GetClosestSlot();
        if (originSlot == null || originSlot.GetItem() == null)
        {
            return false; // there is no item to move
        }
        movingSlot = new SlotClass(originSlot);
        originSlot.Clear();
        isMovingItem = true;
        RefreshUI();
        return true;
    }

    private bool EndItemMove()
    {
        originSlot = GetClosestSlot();
        if (originSlot == null)
        {
            Add(movingSlot.GetItem(), movingSlot.GetQuantity());
            movingSlot.Clear();
        }
        else
        {
            if (originSlot.GetItem() != null)
            {
                if (originSlot.GetItem() == movingSlot.GetItem())
                {
                    if (originSlot.GetItem().isStackable)
                    {
                        originSlot.AddQuantity(movingSlot.GetQuantity());
                        movingSlot.Clear();
                    }
                    else
                        return false;
                }
                else
                {
                    tempSlot = new SlotClass(originSlot); // a = b
                    originSlot.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity()); // b = c
                    movingSlot.AddItem(tempSlot.GetItem(), tempSlot.GetQuantity()); // a = c
                    RefreshUI();
                    return true;
                }
            }
            else //place item as usual
            {
                originSlot.AddItem(movingSlot.GetItem(), movingSlot.GetQuantity());
                movingSlot.Clear();
            }
        }
        isMovingItem = false;
        RefreshUI();
        return true;
    }
    private bool EndItemMove_Single()
    {
        originSlot = GetClosestSlot();
        if (originSlot == null)
            return false;
        if (originSlot.GetItem() != null && originSlot.GetItem() != movingSlot.GetItem())
            return false;

        movingSlot.SubQuantity(1);
        if(originSlot.GetItem() != null && originSlot.GetItem() == movingSlot.GetItem())
            originSlot.AddQuantity(1);
        else 
            originSlot.AddItem(movingSlot.GetItem(), 1);

        if (movingSlot.GetQuantity() < 1)
        {
            isMovingItem = false;
            movingSlot.Clear();
        }
        else
        {
            isMovingItem = true;
        }
        RefreshUI();
        return true;
    }
    private SlotClass GetClosestSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (Vector2.Distance(slots[i].transform.position, Input.mousePosition) <= 28)
            {
                return items[i];
            }
        }
        return null;
    }
    #endregion Moving Items


}

