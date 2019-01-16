using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using System;

public interface IslandGeneratorProperties {}

/**
Main script of every island responsible for making mesh of island
 */
public class Island : MonoBehaviour {
    public int Size = 50;

    public float minPointRadius = 4.0f;
    public int randomPoints = 0;
    public int trianglesInChunk = 500;

    public Material material;

    private OceanManager Ocean;
    private IslandGenerator Generator;
    public Vector2Int WorldGridPosition;
    
    private Random random;

    public List<GameObject> Chunks;

    /* MESH/COLLIDERS GENERATION VARS */

    public IslandGeneratorProperties GeneratorProperties;

    // Elevations at each point in the mesh
    private List<float> elevations;
    private List<Color> islandColors;

    // Fast triangle querier for arbitrary points
    private TriangleBin bin;
    // The delaunay mesh
    private TriangleNet.Mesh mesh = null;

    private List<GameObject> CollidersToUpdate;

    public bool CreateMeshRequest;
    private System.Diagnostics.Stopwatch creatingChunkStopwatch;

    private CustomThreadPool.PoolTask CreatingPoolTask;

    public void Init(OceanManager manager, Vector2Int WorldGridPosition, IslandGenerator Generator) {
        this.Ocean = manager;
        this.WorldGridPosition = WorldGridPosition;
        this.Generator = Generator;

        this.Chunks = new List<GameObject>();
        this.CollidersToUpdate = new List<GameObject>();

        transform.position = new Vector3(WorldGridPosition.x * manager.OneGridPointToWorld, 0, WorldGridPosition.y * manager.OneGridPointToWorld);

        Generator.SetupIslandForGeneration(this);
        Generate(Generator.GetSeed());
    }

    public virtual void Generate(int worldSeed) {
        int chunkSeed = Generator.GetPerIslandSeed(this);

        creatingChunkStopwatch = System.Diagnostics.Stopwatch.StartNew();

        random = new Random(chunkSeed);
        elevations = new List<float>();
        islandColors = new List<Color>();

        CreatingPoolTask = delegate {
            PoissonDiscSampler sampler = new PoissonDiscSampler(Size, Size, minPointRadius);

            Polygon polygon = new Polygon();

            //Add uniformly-spaced points
            foreach (Vector2 sample in sampler.Samples(chunkSeed)) {
                polygon.Add(new Vertex((double)sample.x, (double)sample.y));
            }

            //add points at corners so chunk will be always square shaped
            polygon.Add(new Vertex(0, 0));
            polygon.Add(new Vertex(0, Size));
            polygon.Add(new Vertex(Size, 0));
            polygon.Add(new Vertex(Size, Size));

            //Add some randomly sampled points
            for (int i = 0; i < randomPoints - 4; i++) {
                polygon.Add(new Vertex(random.Range(0.0f, Size), random.Range(0.0f, Size)));
            }

            TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };
            mesh = (TriangleNet.Mesh) polygon.Triangulate(options);

            // Sample perlin noise to get elevations
            foreach (Vertex vert in mesh.Vertices) {
                float height = Generator.GetTerrainHeight((float) vert.x, (float) vert.y, this);
                Color color = Generator.GetTerrainColor((float) vert.x, (float) vert.y, height, this);

                elevations.Add(height);
                islandColors.Add(color);
            }
            
            CreateMeshRequest = true;
        
            //let this be always the last piece of code here
            try {    
                bin = new TriangleBin(mesh, Size, Size, minPointRadius * 2.0f);
            } catch(Exception e) {
                Debug.Log("triangulation failed!");
            }
        };
        CustomThreadPool.AddTask(CreatingPoolTask);
    }

    public void OnDisable() {
        //CustomThreadPool.RemoveTask(CreatingPoolTask);
    }

    public void Update() {
        if(CollidersToUpdate.Count > 0 && Ocean.CanUpdatePartCollider) {
            GameObject toUpdate = CollidersToUpdate[0];

            Mesh mesh = toUpdate.GetComponent<MeshFilter>().mesh;
            toUpdate.GetComponent<MeshCollider>().sharedMesh = mesh;

            CollidersToUpdate.Remove(toUpdate);

            Ocean.CanUpdatePartCollider = false;
        }
    }
    
    public void MakeMesh() {
        IEnumerator<Triangle> triangleEnumerator = mesh.Triangles.GetEnumerator();

        int vertsCount = 0;

        for (int chunkStart = 0; chunkStart < mesh.Triangles.Count; chunkStart += trianglesInChunk) {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();

            int chunkEnd = chunkStart + trianglesInChunk;
            for (int i = chunkStart; i < chunkEnd; i++) {
                if (!triangleEnumerator.MoveNext()) {
                    break;
                }

                Triangle triangle = triangleEnumerator.Current;

                // For the triangles to be right-side up, they need
                // to be wound in the opposite direction
                Vector3 v0 = GetPoint3D(triangle.vertices[2].id);
                Vector3 v1 = GetPoint3D(triangle.vertices[1].id);
                Vector3 v2 = GetPoint3D(triangle.vertices[0].id);

                triangles.Add(vertices.Count);
                triangles.Add(vertices.Count + 1);
                triangles.Add(vertices.Count + 2);

                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                Vector2 uv = new Vector2(v0.x / (float) Size, v0.y / (float) Size);
                uvs.Add(uv);
                uvs.Add(uv);
                uvs.Add(uv);

                colors.Add(islandColors[triangle.vertices[0].id]);
                colors.Add(islandColors[triangle.vertices[0].id]);
                colors.Add(islandColors[triangle.vertices[0].id]);
            }

            Mesh chunkMesh = new Mesh();
            chunkMesh.vertices = vertices.ToArray();
            chunkMesh.uv = uvs.ToArray();
            chunkMesh.triangles = triangles.ToArray();
            chunkMesh.normals = normals.ToArray();
            chunkMesh.colors = colors.ToArray();
            
            chunkMesh.RecalculateBounds();

            vertsCount += vertices.Count;

            GameObject chunk = new GameObject("Chunk");
            chunk.AddComponent<MeshFilter>();
            chunk.AddComponent<MeshCollider>();
            chunk.AddComponent<MeshRenderer>();
            
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            //chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
            chunk.GetComponent<MeshRenderer>().material = material;
            
            chunk.transform.parent = transform;
            chunk.transform.localPosition = new Vector3();

            CollidersToUpdate.Add(chunk);
            Chunks.Add(chunk);
        }

        Debug.Log("Mesh verts count: " + vertsCount);
        
        var b = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>()) {
            if(r != null)
                b.Encapsulate(r.bounds);
        }
        Debug.Log("Chunk bounds: " + b.size);

        Debug.Log("Creating chunk total time: " + creatingChunkStopwatch.ElapsedMilliseconds);
        creatingChunkStopwatch.Stop();
    }

    /* Returns a point's local coordinates. */
    public Vector3 GetPoint3D(int index) {
        try {
        Vertex vertex = mesh.vertices[index];
        float elevation = elevations[index];
        return new Vector3((float)vertex.x, elevation, (float)vertex.y);
        } catch(Exception e) {
            return default(Vector3);
        }
    }
    
    /* Returns the triangle containing the given point. If no triangle was found, then null is returned.
       The list will contain exactly three point indices. */
    public List<int> GetTriangleContainingPoint(Vector2 point) {
        Triangle triangle = bin.getTriangleForPoint(new Point(point.x, point.y));
        if (triangle == null) {
            return null;
        }

        return new List<int>(new int[] { triangle.vertices[0].id, triangle.vertices[1].id, triangle.vertices[2].id });
    }

    /* Returns a pretty good approximation of the height at a given point in worldspace */
    public float GetElevation(float x, float y) {
        if (x < 0 || x > Size ||
                y < 0 || y > Size) {
            return 0.0f;
        }

        Vector2 point = new Vector2(x, y);
        List<int> triangle = GetTriangleContainingPoint(point);

        if (triangle == null) {
            // This can happen sometimes because the triangulation does not actually fit entirely within the bounds of the grid;
            // not great error handling, but let's return an invalid value
            return float.MinValue;
        }

        Vector3 p0 = GetPoint3D(triangle[0]);
        Vector3 p1 = GetPoint3D(triangle[1]);
        Vector3 p2 = GetPoint3D(triangle[2]);

        Vector3 normal = Vector3.Cross(p0 - p1, p1 - p2).normalized;
        float elevation = p0.y + (normal.x * (p0.x - x) + normal.z * (p0.z - y)) / normal.y;

        return elevation;
    }
}