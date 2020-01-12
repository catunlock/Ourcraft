using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;



public class Chunk : MonoBehaviour {
    public Vector3Int coord;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    [HideInInspector]
    public Mesh mesh;
    [HideInInspector] 
    public ComputeBuffer pointsBuffer;
    [HideInInspector]
    public int numPoints;

    enum EntityType { Tree, House, Zombie, Animal};

    [Serializable]
    class EntityData
    {
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public EntityType entityType;

        public EntityData(Vector3 position, Quaternion rotation, EntityType entityType)
        {
            this.position = position;
            this.rotation = rotation;
            this.entityType = entityType;
        }
    };

    List<EntityData> entities;

    [Serializable]
    class ChunkData
    {
        public int numPoints;
        public float[] realPointsBuffer;
        public EntityData[] entityData;
    };


    public static HashSet<Vector3Int> getSavedChunks()
    {
        HashSet<Vector3Int> result = new HashSet<Vector3Int>();

        String destination = Application.persistentDataPath + "/";
        foreach(String filepath in Directory.GetFiles(destination))
        {
            String[] sCoords = filepath.Split('.')[0].Split('_');
      
            if (sCoords[0].Contains("chunk"))
            {
                result.Add(new Vector3Int(Int32.Parse(sCoords[1]), Int32.Parse(sCoords[2]), Int32.Parse(sCoords[3])));
            }
            
        }

        return result;
    }

    protected void InitTrees()
    {
        TreeGenerator treeGenerator = FindObjectOfType<TreeGenerator>();

        for (int x = 0; x < UnityEngine.Random.Range(0, 5); x++)
        {
            float posX = UnityEngine.Random.Range(coord.x * 10.0f - 5, coord.x * 10.0f + 5);
            float posZ = UnityEngine.Random.Range(coord.z * 10.0f - 5, coord.z * 10.0f + 5);

            Ray ray = new Ray(new Vector3(posX, (coord.y * 10 - 5), posZ), new Vector3(0, -1, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 10))
            {
                if (hit.transform.tag.Equals("chunk") && hit.point.y > -1 && hit.point.y < 5)
                {
                    // if (hit.point > 10 && hit.point < 100)
                    Vector3 position = hit.point;
                    GameObject treeType = treeGenerator.shortTrees[UnityEngine.Random.Range(0, treeGenerator.shortTrees.Length)];
                    Quaternion rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0));

                    GameObject treeObject = Instantiate(treeType, position, rotation);
                    treeObject.transform.parent = this.transform;

                    entities.Add(new EntityData(position, rotation, EntityType.Tree));
                }
            }
        }
    }

    protected void InitAnimals()
    {
        AnimalGenerator animalGenerator = FindObjectOfType<AnimalGenerator>();

        for (int x = 0; x < UnityEngine.Random.Range(0, 3); x++)
        {
            float posX = UnityEngine.Random.Range(coord.x * 10.0f - 5, coord.x * 10.0f + 5);
            float posZ = UnityEngine.Random.Range(coord.z * 10.0f - 5, coord.z * 10.0f + 5);

            Ray ray = new Ray(new Vector3(posX, (coord.y * 10 - 5), posZ), new Vector3(0, -1, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 10))
            {
                if (hit.transform.tag.Equals("chunk") && hit.point.y > -1 && hit.point.y < 5)
                {
                    Vector3 position = hit.point;
                    GameObject animalPrefab = animalGenerator.animal;
                    Quaternion rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0));

                    GameObject animal = Instantiate(animalPrefab, position, rotation);
                    animal.transform.parent = this.transform;

                    entities.Add(new EntityData(position, rotation, EntityType.Animal));
                }
            }
        }
    }

    protected void InitZombies()
    {
        ZombieGenerator zombieGenerator = FindObjectOfType<ZombieGenerator>();

        for (int x = 0; x < UnityEngine.Random.Range(0, 2); x++)
        {
            float posX = UnityEngine.Random.Range(coord.x * 10.0f - 5, coord.x * 10.0f + 5);
            float posZ = UnityEngine.Random.Range(coord.z * 10.0f - 5, coord.z * 10.0f + 5);

            Ray ray = new Ray(new Vector3(posX, (coord.y * 10 - 5), posZ), new Vector3(0, -1, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 10))
            {
                if (hit.transform.tag.Equals("chunk") && hit.point.y > -1 && hit.point.y < 5)
                {
                    Vector3 position = hit.point;
                    Quaternion rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0));
                    entities.Add(new EntityData(position, rotation, EntityType.Zombie));
                }
            }
        }
    }

    public void SpawnZombies()
    {
        ZombieGenerator zombieGenerator = FindObjectOfType<ZombieGenerator>(); ;

        foreach (EntityData entity in entities)
        {
            if (entity.entityType == EntityType.Zombie)
            {
                Vector3 position = entity.position;
                Quaternion rotation = entity.rotation;
                GameObject zombiePrefab = zombieGenerator.zombie;
                GameObject zombie = Instantiate(zombiePrefab, position, rotation);
                break;
            }
        }
    }

    public void InitEntities()
    {
        entities = new List<EntityData>();
        InitTrees();
        InitZombies();
        InitAnimals();
    }

    public static String getFileName(Vector3Int coord)
    {
        return "chunk_" + coord.x + "_" + coord.y + "_" + coord.z + ".dat";
    }

    public void Save()
    {    
        BinaryFormatter bf = new BinaryFormatter();
        String fileName = "chunk_" + coord.x + "_" + coord.y + "_"+ coord.z + ".dat";
        String destination = Application.persistentDataPath + "/" + fileName;
        FileStream file = File.Open(destination, FileMode.OpenOrCreate);

        ChunkData chunkData = new ChunkData();
        chunkData.numPoints = numPoints;
        chunkData.realPointsBuffer = new float[numPoints];
        chunkData.entityData = entities.ToArray();
        pointsBuffer.GetData(chunkData.realPointsBuffer, 0, 0, numPoints);
        bf.Serialize(file, chunkData);
        file.Close();
        Debug.Log(destination + " saved");
    }

    public void Load()
    {
        BinaryFormatter bf = new BinaryFormatter();
        String fileName = "chunk_" + coord.x + "_" + coord.y + "_" + coord.z + ".dat";
        String destination = Application.persistentDataPath + "/" + fileName;
        FileStream file;

        if (File.Exists(destination)) file = File.Open(destination, FileMode.Open);
        else
        {
            Debug.LogError("File not found");
            return;
        }

        ChunkData chunkData = bf.Deserialize(file) as ChunkData;
        file.Close();

        numPoints = chunkData.numPoints;
        entities = new List<EntityData>(chunkData.entityData);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        pointsBuffer.SetData(chunkData.realPointsBuffer);
        mesh = new Mesh();
        Debug.Log(destination + " loaded");
        
        // Load entities
        TreeGenerator treeGenerator = FindObjectOfType<TreeGenerator>();
        AnimalGenerator animalGenerator = FindObjectOfType<AnimalGenerator>();

        foreach (EntityData entity in entities)
        {
            Vector3 position = entity.position;
            Quaternion rotation = entity.rotation;

            switch (entity.entityType)
            {
                case EntityType.Tree:
                    GameObject treeType = treeGenerator.shortTrees[UnityEngine.Random.Range(0, treeGenerator.shortTrees.Length)];
                    GameObject treeObject = Instantiate(treeType, position, rotation);
                    treeObject.transform.parent = this.transform;
                    break;

                case EntityType.Animal:
                    GameObject animalPrefab = animalGenerator.animal;
                    GameObject animal = Instantiate(animalPrefab, position, rotation);
                    animal.transform.parent = this.transform;
                    break;
            }
           
        }
        
    }

    public void DestroyOrDisable () {
        if (Application.isPlaying) {
            mesh.Clear ();
            gameObject.SetActive (false);
        } else {
            DestroyImmediate (gameObject, false);
        }
    }

    private void OnDestroy()
    {
        if (pointsBuffer != null) {
            pointsBuffer.Release();
        }
        if (mesh != null)
        {
            if (meshCollider != null)
            {
                Destroy(meshCollider.sharedMesh);
            }
            
            if (meshFilter != null)
            {
                Destroy(meshFilter.sharedMesh);
            }
            
            Destroy(mesh);
        }
        
    }

    // Add components/get references in case lost (references can be lost when working in the editor)
    public void SetUp (Material mat) {

        meshFilter = GetComponent<MeshFilter> ();
        meshRenderer = GetComponent<MeshRenderer> ();
        meshCollider = GetComponent<MeshCollider> ();
        pointsBuffer = null;

        if (meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter> ();
        }

        if (meshRenderer == null) {
            meshRenderer = gameObject.AddComponent<MeshRenderer> ();
        }

        if (meshCollider == null) {
            meshCollider = gameObject.AddComponent<MeshCollider> ();
        }
       
        mesh = meshFilter.sharedMesh;
        if (mesh == null) {
            mesh = new Mesh ();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        if (meshCollider.sharedMesh == null) {
            meshCollider.sharedMesh = mesh;
        }

        meshRenderer.material = mat;
    }
}