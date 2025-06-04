using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetSpawner : MonoBehaviour
{

    public GameObject NPC;
    public GameObject building;
    public GameObject book;

    private Vector3 NPCpos = new Vector3(-7703.52f,2.54f,1353.17f);
    private Vector3 buildingPos = new Vector3(-7703.52f,2.53f,1353.17f);
    private Vector3 bookPos = new Vector3(-7731.293f,2f,1379.011f);
    
    private Quaternion spawnRotation =  Quaternion.identity;
    
    // Start is called before the first frame update
    void Start()
    {
        SpawnObjects();
        
    }

    void SpawnObjects()
    {
        GameObject npcInstance = Instantiate(NPC, NPCpos, spawnRotation);
        SetupInteractionZone(npcInstance, new Vector3(4f, 3f, 4f), new Vector3(0f, 1.5f, 0f), "Interactable");

        GameObject buildingInstance = Instantiate(building, buildingPos, spawnRotation);
        SetupInteractionZone(buildingInstance, new Vector3(6f, 4f, 6f), new Vector3(0f, 2f, 0f), "Interactable");

        GameObject bookInstance = Instantiate(book, bookPos, spawnRotation);
        SetupInteractionZone(bookInstance, new Vector3(2f, 2f, 2f), new Vector3(0f, 1f, 0f), "Interactable");
    }


    void SetupInteractionZone(GameObject obj, Vector3 colliderSize, Vector3 colliderCenter, string tagName)
    {
        BoxCollider collider = obj.GetComponent<BoxCollider>();
        
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;
        collider.size = colliderSize;
        collider.center = colliderCenter;

        if (!string.IsNullOrEmpty(tagName))
        {
            obj.tag = tagName;
        }
    }

}
