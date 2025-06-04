using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    
    private GameObject currentInteractable;

    public static bool NPCkilled = false;
    
    
    // Start is called before the first frame update
    void Start()
    {
        // Ensure the player has a Rigidbody
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // Prevent physics movement
        }

        // Ensure the player has a Collider
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = false;
        }
    }
    
    void Update()
    {
        if (currentInteractable != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                InteractionAction();
            }
            
            if (currentInteractable.name.Contains("NPC(Clone)") && Input.GetKey(KeyCode.K))
            {
                Debug.Log("NPC destroyed.");
                Destroy(currentInteractable);
                currentInteractable = null;
                
                NPCkilled = true;
                FindObjectOfType<MapGenerator>().DrawMapInEditor();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            currentInteractable = other.gameObject;
            Debug.Log("Entered interaction zone: " + currentInteractable.name);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentInteractable)
        {
            Debug.Log("Exited interaction zone: " + other.name);
            currentInteractable = null;
        }
    }

    void InteractionAction()
    {
        if (currentInteractable == null)
        {
            Debug.LogWarning("Tried to interact, but no object is assigned.");
            return;
        }
        
        if (currentInteractable.name.Contains("NPC(Clone)"))
        {
            Debug.Log("Talk to NPC...");
        }
        else if (currentInteractable.name.Contains("Book(Clone)"))
        {
            Debug.Log("Read the book...");
            
            // Scale player
            //this.gameObject.transform.localScale = new Vector3(1f, 20f, 1f);
            
            // Move player up so it doesn't go through the ground
            this.gameObject.transform.position += new Vector3(0, 50f, 0);
            
        }
    }
}
