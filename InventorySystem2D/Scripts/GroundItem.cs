using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GroundItem : MonoBehaviour
{
    public ItemClass itemClass;
    private InventoryManager inventory;
    public bool nearPlayer;

    private void Start()
    {
        GameEvents.current.onItemPickup += ItemPickup;
        inventory = GameObject.Find("Inventory").GetComponent<InventoryManager>();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        { nearPlayer = true;
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        { nearPlayer = false; }
    }
    public void ItemPickup()
    {
        var item = this;

        if (item && item.nearPlayer)
        {
            if(Input.GetKeyDown(KeyCode.E))
            { 
            inventory.Add(itemClass.GetItem(), 1);
            item.nearPlayer = false;
            Destroy(item.gameObject);
            }
        }
    }
}
