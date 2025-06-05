using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionController : MonoBehaviour
{

    public Texture2D questMarkerTexture;
    public static bool NPCkilled = false;
    public static bool doomRealm = false;
    
    private GameObject questMarker;
    private GameObject currentInteractable;

    private bool pressedE = false;
    
    // Start is called before the first frame update
    void Start()
    {
        questMarker = CovertToSprite(questMarkerTexture);
        
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
            InteractionAction();
            
            if (currentInteractable.name.Contains("NPC(Clone)") && Input.GetKey(KeyCode.K))
            {
                Debug.Log("NPC destroyed.");
                Destroy(currentInteractable);
                currentInteractable = null;
                NPCkilled = true;
                
                InitiateDoomQuest();
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

        if (currentInteractable.name.Contains("QuestMarker"))
        {
            if (NPCkilled)
            {
                Debug.Log("Doom Realm Transformation");
                doomRealm =  true;
                FindObjectOfType<MapGenerator>().DrawMapInEditor();
            }
            else
            {
                Debug.Log("-===========================================-");
                Debug.Log("-=| You See How Beautiful The View Is!");
                Debug.Log("-===========================================-");
            }
        }
        
        if (currentInteractable.name.Contains("NPC(Clone)"))
        {
            if (!pressedE)
            {
                Debug.Log("-===========================================-");
                Debug.Log("-=| Press 'E' to Talk To NPC");
                Debug.Log("-=| Or 'K' to Kill NPC");
                Debug.Log("-===========================================-");
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                pressedE = true;
                
                SpawnQuestMarker(questMarker, new Vector3(-7626.1f, 19.6f, 1477f), new Vector3(6f, 16.79725f, 6f), new Vector3(0f, -4.398627f, 0f));
            
                Debug.Log("-===========================================-");
                Debug.Log("-=| Enjoy The View From The Top of That Mountain");
                Debug.Log("-===========================================-");
            }
        }
        else if (currentInteractable.name.Contains("Book(Clone)"))
        {
            Debug.Log("Read the book...");
            
            // Move player up so it doesn't go through the ground
            this.gameObject.transform.position += new Vector3(0, 35f, 0);
            
            // Scale player
            this.gameObject.transform.localScale = new Vector3(1f, 20f, 1f);
            
            StartCoroutine(DestroyAfterDelay(currentInteractable, 1f));
            
            Debug.Log("-=======================================================-");
            Debug.Log("-=| You Have Read And Practiced The Spell of The Giants");
            Debug.Log("-=| Is It a Blessing Or a Curse");
            Debug.Log("-=======================================================-");
            
        }
    }
    
    public GameObject  CovertToSprite(Texture2D texture)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f) // pivot in center
        );

        GameObject questMarker = new GameObject("QuestMarker");
        var renderer = questMarker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        
        return questMarker;
    }

    void SpawnQuestMarker(GameObject obj, Vector3 pos, Vector3 colliderSize, Vector3 colliderCenter)
    {
        questMarker.transform.position = pos; // position in scene
        obj.SetActive(true);
        
        AssetSpawner.SetupInteractionZone(obj,colliderSize,colliderCenter, "Interactable" );
    }

    void InitiateDoomQuest()
    {
        SpawnQuestMarker(questMarker, new Vector3(-7702.5f, 12.5f, 1322.8f), new Vector3(20.87939f, 16.62999f, 24.39624f), new Vector3(-0.9001465f, -4.315f, -0.8531494f));
        
        Debug.Log("-===========================================-");
        Debug.Log("-=| Secrifice Your Self In The Water To Change The Water Into Lava");
        Debug.Log("-=| and Enter the Doom Realm");
        Debug.Log("-===========================================-");
        
    }
    
    IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
    
        if (obj != null)
        {
            Destroy(obj);
            if (currentInteractable == obj)
            {
                currentInteractable = null;
            }
        }
    }
}
