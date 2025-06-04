using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float HeightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        
        int meshSimplicitaionIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        
        int borderdSize = heightMap.GetLength(0);
        int meshSize = borderdSize - 2 * meshSimplicitaionIncrement;
        int meshSizeUnsimplified = borderdSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        
        int verticesPerLine = (meshSize-1)/meshSimplicitaionIncrement + 1;
        
        MeshData meshData = new MeshData(verticesPerLine);
        
        int[,] vertexIndicesMap = new int[borderdSize, borderdSize];
        
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderdSize; y += meshSimplicitaionIncrement)
        {
            for (int x = 0; x < borderdSize; x += meshSimplicitaionIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderdSize - 1 || x == 0 || x == borderdSize - 1;;

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    {
                        vertexIndicesMap[x, y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }
        }

        for (int y = 0; y < borderdSize; y+= meshSimplicitaionIncrement)
        {
            for (int x = 0; x < borderdSize; x+= meshSimplicitaionIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                
                Vector2 percent = new Vector2((x-meshSimplicitaionIncrement) / (float)meshSize, (y-meshSimplicitaionIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * HeightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height , topLeftZ - percent.y * meshSizeUnsimplified); // vertacies creation
                
                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                
                if (x < borderdSize - 1 && y < borderdSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplicitaionIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplicitaionIncrement];
                    int d = vertexIndicesMap[x + meshSimplicitaionIncrement, y + meshSimplicitaionIncrement];
                    
                    meshData.AddTriangle(a,d,c); // adding triangles to each square
                    meshData.AddTriangle(d,a,b);
                }
                
                vertexIndex++;
            }
        }
        meshData.BakeNormals();
        
        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;
    
    Vector3[] borderVertices;
    int[] borderTriangles;
    
    int triangleIndex;
    int borderTriangleIndex;

    public MeshData(int VerticesPerLine)
    {
        vertices = new Vector3[VerticesPerLine * VerticesPerLine];
        uvs = new Vector2[VerticesPerLine * VerticesPerLine];
        triangles = new int[(VerticesPerLine - 1) * (VerticesPerLine - 1) * 6];
        
        borderVertices = new Vector3[VerticesPerLine * 4 + 4];
        borderTriangles = new int[24 * VerticesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int VertexIndex)
    {
        if (VertexIndex < 0)
        {
            borderVertices[-VertexIndex -1] = vertexPosition;
        }
        else
        {
            vertices[VertexIndex] = vertexPosition;
            uvs[VertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];
            
            Vector3 triangleNormal = SurfaceNormalFormIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];
            
            Vector3 triangleNormal = SurfaceNormalFormIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }

            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }

            if (vertexIndexC >= 0)
            { 
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }
        
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFormIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];
        
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }
    
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;
        return mesh;
    }
    
}
