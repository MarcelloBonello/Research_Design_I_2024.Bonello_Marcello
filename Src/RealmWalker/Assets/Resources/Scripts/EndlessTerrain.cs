using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float scale = 1f;
    const float viewerMoveThresholdForChunkUpdate = 25f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    
    public LODinfo[] detailLevels;
    public static float maxViewDst;
    
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;
    
    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terraubChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize -1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new  Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {

        for (int i = 0; i < terraubChunksVisibleLastUpdate.Count; i++)
        {
            terraubChunksVisibleLastUpdate[i].SetVisible(false);
        }

        terraubChunksVisibleLastUpdate.Clear();
        
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewdChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewdChunkCoord))
                {
                    terrainChunkDict[viewdChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDict.Add(viewdChunkCoord, new TerrainChunk(viewdChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;
        
        LODinfo[] detailLevels;
        LODmesh[] lodMeshes;
        private LODmesh collisionLODmesh;
        
        MapData mapData;
        bool mapDataRecived;
        int previousLODindex = -1;
        
        public TerrainChunk(Vector2 coord, int size,LODinfo[] detailLevels, Transform parent, Material material)
        {
            
            this.detailLevels = detailLevels;
            
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);
            
            lodMeshes = new LODmesh[detailLevels.Length];

            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODmesh(detailLevels[i].lod, UpdateTerrainChunk);

                if (detailLevels[i].useForCollider)
                {
                    collisionLODmesh = lodMeshes[i];
                }
            }
            
            mapGenerator.RequestMapData(position, OnMapDataRecived);
        }

        void OnMapDataRecived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecived = true;
            
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            
            UpdateTerrainChunk();
        }

        void OnMeshDataRecived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
            
        }
        
        public void UpdateTerrainChunk()
        {
            if (mapDataRecived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;
                
                    for (int i = 0; i < detailLevels.Length -1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODindex)
                    {
                        LODmesh lodMesh = lodMeshes[lodIndex];
                    
                        if (lodMesh.hasMesh)
                        {
                            previousLODindex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;

                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    if (lodIndex == 0)
                    {
                        if (collisionLODmesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODmesh.mesh;
                        }
                        else if (!collisionLODmesh.hasRequestedMesh)
                        {
                            collisionLODmesh.RequestMesh(mapData);
                        }
                    }
                    terraubChunksVisibleLastUpdate.Add(this);
                }
                SetVisible(visible);
            }
            
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODmesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;
        private System.Action updateCallBack;

        public LODmesh(int lod, System.Action updateCallBack)
        {
            this.lod = lod;
            this.updateCallBack = updateCallBack;
        }

        void OnMeshDataRecived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            
            updateCallBack();
        }
        
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecived);
        }
    }

    [System.Serializable]
    public struct LODinfo
    {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;

    }
}
