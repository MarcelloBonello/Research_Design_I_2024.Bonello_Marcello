using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
    
    public void DrawMesh(MeshData meshdata, Texture2D texture)
    {
        meshFilter.sharedMesh = meshdata.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
