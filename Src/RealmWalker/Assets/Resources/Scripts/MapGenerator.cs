using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using System;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        DrawMesh,
        FallOffMap
    };
    
    public Noise.NormalizeMode normalizeMode;

    public DrawMode drawMode;
    
    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int editorPreviewLOD;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public int seed;
    public Vector2 offset;

    public bool useFallOff; 
    public bool autoUpdate;
    bool hasUpdatedColor = false;

    public TerrainType[] regions;
    
    float[,] fallOffMap;
    
    Queue<MapThreadInfo<MapData>> MapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> MeshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake()
    {
        fallOffMap = FallOffGenerator.GenerateFallOffMap(mapChunkSize);
    }
    
    void Update()
    {
        if (MapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < MapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = MapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (MeshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < MeshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = MeshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        
        if (InteractionController.NPCkilled && !hasUpdatedColor)
        {
            DrawMapInEditor();
            hasUpdatedColor = true;
        }
    }
    
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.DrawMesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD),
                TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FallOffGenerator.GenerateFallOffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (MapDataThreadInfoQueue)
        {
            MapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData,int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod,  callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData,int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (MeshDataThreadInfoQueue)
        {
            MeshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }
    
    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance,
            lacunarity, centre + offset, normalizeMode);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        
        if (InteractionController.NPCkilled && !hasUpdatedColor)
        {
            Debug.Log("NPC killed — changing region colors");

            Color DeepWaterColor = new Color(0.3f, 0.0f, 0.0f);
            Color WaterColor     = new Color(0.7f, 0.0f, 0.0f);
            Color SandColor      = new Color(0.22f, 0.21f, 0.2f);
            Color GrassColor     = new Color(0.2f, 0.22f, 0.13f);
            Color Grass2Color    = new Color(0.29f, 0.25f, 0.18f);
            Color RockColor      = new Color(0.13f, 0.09f, 0.10f);
            Color Rock2Color     = new Color(0.12f, 0.12f, 0.12f);
            Color SnowColor      = new Color(0.6f, 0.2f, 0.0f);

            for (int i = 0; i < regions.Length; i++)
            {
                switch (regions[i].name)
                {
                    case "DeepWater": regions[i].color = DeepWaterColor; break;
                    case "Water":     regions[i].color = WaterColor;     break;
                    case "Sand":      regions[i].color = SandColor;     break;
                    case "Grass":     regions[i].color = GrassColor;     break;
                    case "Grass2":    regions[i].color = Grass2Color;    break;
                    case "Rock":      regions[i].color = RockColor;      break;
                    case "Rock2":     regions[i].color = Rock2Color;     break;
                    case "Snow":      regions[i].color = SnowColor;      break;
                }
            }

            hasUpdatedColor = true; // So we don't do it again
        }

        
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFallOff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - fallOffMap[x, y]);
                }
                
                float currentHeight = noiseMap[x, y];
                
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colourMap);
    }

    void OnValidate() // called when any value is changed in the inspector
    {
        //if(mapChunkSize < 1) mapChunkSize = 1;
        //if(mapChunkSize < 1) mapChunkSize = 1;
        if(lacunarity < 1) lacunarity = 1;
        if(octaves < 0) octaves = 1;
        
        fallOffMap = FallOffGenerator.GenerateFallOffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
        
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color; 
}

public struct MapData
{
    public readonly float [,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
