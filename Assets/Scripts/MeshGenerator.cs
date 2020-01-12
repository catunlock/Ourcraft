using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour {

    const int threadGroupSize = 8;

    [Header ("General Settings")]
    public DensityGenerator densityGenerator;

    public Transform viewer;
    public float viewDistance = 30;

    [Space ()]
    public bool autoUpdateInEditor = true;
    public bool autoUpdateInGame = true;
    public ComputeShader shader;
    public ComputeShader modifyShader;
    public Material mat;

    [Header ("Voxel Settings")]
    public float isoLevel;
    public float boundsSize = 1;
    public Vector3 offset = Vector3.zero;

    [Range (2, 100)]
    public int numPointsPerAxis = 30;

    [Header ("Gizmos")]
    public bool showBoundsGizmo = true;
    public Color boundsGizmoCol = Color.white;

    GameObject chunkHolder;
    const string chunkHolderName = "Chunks Holder";
    List<Chunk> chunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    HashSet<Vector3Int> modifiedChunks;
    HashSet<Vector3Int> savedChunks;
    HashSet<Vector3Int> loadingChunks;


    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;

    float timeLastChunkSearch;
    float timeSinceUpdate;

    bool settingsUpdated;

    void Awake () {
        if (Application.isPlaying) {
            InitVariableChunkStructures ();

            var oldChunks = FindObjectsOfType<Chunk> ();
            for (int i = oldChunks.Length - 1; i >= 0; i--) {
                Destroy (oldChunks[i].gameObject);
            }
        }
    }

    void Update () {
        // Update endless terrain
        if (Application.isPlaying) {
            timeSinceUpdate = Time.realtimeSinceStartup;
            Run ();
        }

        if (settingsUpdated) {
            RequestMeshUpdate ();
            settingsUpdated = false;
        }
    }

    internal Vector3Int ChunkCoordFromPos(Vector3 pos)
    {
        Vector3 ps = pos / boundsSize;
        return new Vector3Int(Mathf.RoundToInt(ps.x), Mathf.RoundToInt(ps.y), Mathf.RoundToInt(ps.z));
    }
    internal void UpdateChunk(Vector3 hitPoint, float force, float range)
    {
        HashSet<Vector3Int> chunksToModify = new HashSet<Vector3Int>();
        float[] offsets = { -range, 0, range };

        foreach (float x in offsets)
        {
            foreach (float y in offsets)
            {
                foreach (float z in offsets)
                {
                    Vector3 offset = new Vector3(x, y, z);
                    Vector3Int chunkCoord = ChunkCoordFromPos(hitPoint + offset);
                    chunksToModify.Add(chunkCoord);
                    modifiedChunks.Add(chunkCoord);
                }
            }
        }

        foreach (Vector3Int posChunk in chunksToModify)
        {
            if (!existingChunks.ContainsKey(posChunk))
            {
                Debug.LogWarning("Denied modification: All chunks have to be loaded");
                return;
            }
        }
            

        foreach (Vector3Int posChunk in chunksToModify)
        {
            
            Chunk chunk = existingChunks[posChunk];
            Vector3 centre = CentreFromCoord(chunk.coord);
            float spacing = boundsSize / (numPointsPerAxis - 1);

            int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);
            // Points buffer is populated inside shader with pos (xyz) + density (w).
            // Set paramaters
            modifyShader.SetBuffer(0, "points", chunk.pointsBuffer);
            modifyShader.SetVector("hitpos", new Vector4(hitPoint.x, hitPoint.y, hitPoint.z));
            modifyShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            modifyShader.SetFloat("boundsSize", boundsSize);
            modifyShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
            modifyShader.SetFloat("spacing", spacing);
            modifyShader.SetFloat("radius", range);
            modifyShader.SetFloat("force", force);

            // Dispatch shader
            modifyShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            UpdateChunkMesh(chunk);
        }
    }

    public void Run () {
        CreateBuffers ();

        if (Application.isPlaying) {
            InitVisibleChunks ();
            perFrameChunkLoad();
            timeLastChunkSearch += Time.deltaTime;
        }

        // Release buffers immediately in editor
        if (!Application.isPlaying) {
            ReleaseBuffers ();
        }

    }

    public void RequestMeshUpdate () {
        if ((Application.isPlaying && autoUpdateInGame) || (!Application.isPlaying && autoUpdateInEditor)) {
            Run ();
        }
    }

    void InitVariableChunkStructures () {
        chunks = new List<Chunk> ();
        existingChunks = new Dictionary<Vector3Int, Chunk> ();
        savedChunks = Chunk.getSavedChunks();
        modifiedChunks = new HashSet<Vector3Int>();
        loadingChunks = new HashSet<Vector3Int>();
    }

    private void OnApplicationQuit()
    {
        foreach(Vector3Int coord in modifiedChunks)
        {
            if (existingChunks.ContainsKey(coord))
            {
                Chunk chunk = existingChunks[coord];
                chunk.Save();
            }
        }
    }

    void InitVisibleChunks () {
        if (chunks==null) {
            return;
        }
        CreateChunkHolder ();
        Vector3Int viewerCoord = ChunkCoordFromPos(viewer.position);

        if (timeLastChunkSearch <= 1.0)
        {
            return;
        }
        timeLastChunkSearch = 0;

        int maxChunksInView = Mathf.CeilToInt (viewDistance / boundsSize);
        float sqrViewDistance = viewDistance * viewDistance;

        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = chunks.Count - 1; i >= 0; i--) {
            Chunk chunk = chunks[i];
            Vector3 centre = CentreFromCoord (chunk.coord);
            Vector3 viewerOffset = viewer.position - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance) {
                if (modifiedChunks.Contains(chunk.coord))
                {
                    chunk.Save();
                    savedChunks.Add(chunk.coord);
                    modifiedChunks.Remove(chunk.coord);
                }
                existingChunks.Remove (chunk.coord);
                chunks.RemoveAt (i);
                Destroy(chunk.gameObject);
            }
        }

        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z) + viewerCoord;

                    if (existingChunks.ContainsKey (coord) || loadingChunks.Contains(coord)) {
                        continue;
                    }

                    Vector3 centre = CentreFromCoord (coord);
                    Vector3 viewerOffset = viewer.position - centre;
                    Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
                    float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance) {
                        loadingChunks.Add(coord);
                        /*
                        Bounds bounds = new Bounds (CentreFromCoord (coord), Vector3.one * boundsSize);
                        if (IsVisibleFrom (bounds, Camera.main)) {
                            loadingChunks.Add(coord);
                        }
                        */
                    }

                }
            }
        }
    }

    public void perFrameChunkLoad()
    {
        Vector3Int cameraChunk = ChunkCoordFromPos(viewer.transform.position);
        if (loadingChunks.Count == 0)
        {
            return;
        }

        HashSet<Vector3Int> removeCoords = new HashSet<Vector3Int>();

        
        foreach (Vector3Int coord in loadingChunks)
        {
            Chunk chunk = CreateChunk(coord);
            chunk.SetUp(mat);
            if (savedChunks.Contains(coord))
            {
                chunk.Load();
            } else
            {
                chunk.InitEntities();
            }
            existingChunks.Add(coord, chunk);
            chunks.Add(chunk);
            UpdateChunkMesh(chunk);
            removeCoords.Add(coord);
            chunk.gameObject.tag = "chunk";
            chunk.gameObject.layer = LayerMask.NameToLayer("Ground");
            
            if (Time.realtimeSinceStartup - timeSinceUpdate > (1.0/90.0))
            {
                break;
            }
            
        }

        loadingChunks.ExceptWith(removeCoords);
        
        
    }

    public bool IsVisibleFrom (Bounds bounds, Camera camera) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (planes, bounds);
    }


    public void UpdateChunkMesh (Chunk chunk) {

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);
        if (chunk.pointsBuffer == null)
        {
            int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;

            ComputeBuffer pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));

            densityGenerator.Generate(pointsBuffer, numPointsPerAxis, boundsSize, centre, offset, pointSpacing);
            chunk.pointsBuffer = pointsBuffer;
            chunk.numPoints = numPoints;
        }

        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(0, "points", chunk.pointsBuffer);
        shader.SetBuffer(0, "triangles", triangleBuffer);
        shader.SetInt("numPointsPerAxis", numPointsPerAxis);
        shader.SetFloat("isoLevel", isoLevel);
        shader.SetFloat("boundsSize", boundsSize);
        shader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        shader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        shader.SetFloat("spacing", pointSpacing);

        shader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
        mesh.Optimize();

        MeshFilter meshFilter = chunk.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = chunk.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

    }

    public void UpdateAllChunks () {

        // Create mesh for each chunk
        foreach (Chunk chunk in chunks) {
            UpdateChunkMesh (chunk);
        }

    }

    void OnDestroy () {
        if (Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    void CreateBuffers () {
        
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || triangleBuffer == null) {
            if (Application.isPlaying) {
                ReleaseBuffers ();
            }
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        }
    }

    void ReleaseBuffers () {
        if (triangleBuffer != null) {
            triangleBuffer.Release ();
            triCountBuffer.Release ();
        }
    }

    Vector3 CentreFromCoord (Vector3Int coord) {
        return new Vector3 (coord.x, coord.y, coord.z) * boundsSize;
    }

    void CreateChunkHolder () {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null) {
            if (GameObject.Find (chunkHolderName)) {
                chunkHolder = GameObject.Find (chunkHolderName);
            } else {
                chunkHolder = new GameObject (chunkHolderName);
            }
        }
    }

    Chunk CreateChunk (Vector3Int coord) {
        GameObject chunk = new GameObject ($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk> ();
        newChunk.coord = coord;
        return newChunk;
    }

    void OnValidate() {
        settingsUpdated = true;
    }

    struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    void OnDrawGizmos () {
        if (showBoundsGizmo) {
            Gizmos.color = boundsGizmoCol;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk> (FindObjectsOfType<Chunk> ()) : this.chunks;
            foreach (var chunk in chunks) {
                Bounds bounds = new Bounds (CentreFromCoord (chunk.coord), Vector3.one * boundsSize);
                Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube (CentreFromCoord (chunk.coord), Vector3.one * boundsSize);
            }
        }
    }

    public void SpawnZombies()
    {
        Debug.Log("The dead are rising...");
        foreach (Chunk chunk in chunks)
        {
            chunk.SpawnZombies();
        }
    }

    public void DespawnZombies()
    {
        Debug.Log("The dead are ... dead");
        Zombie[] zombies = GameObject.FindObjectsOfType<Zombie>();

        foreach (Zombie zombie in zombies)
        {
            zombie.dead = true;
            Destroy(zombie.gameObject, 5);
        }
    }
}