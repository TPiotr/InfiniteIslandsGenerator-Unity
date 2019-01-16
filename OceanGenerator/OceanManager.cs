using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Main class responsible for managing islands (generate them or destroy)
 */
public class OceanManager : MonoBehaviour
{
	private static System.Object ThreadLock = new System.Object();

	public int OneGridPointToWorld = 10;
    public int Range = 2; //world visiblity (higher val == more islands in memory)
    public int DeleteDistance = 10; //distance after which island is going to be deleted

    public OceanGenerator OceanGenerator; //generate used to determine where islands should be 
	public Material material;
	public Transform WorldCamera;

    [HideInInspector]
    public List<Island> Islands;

    private Vector2Int CurrentCameraChunkPosition, LastCameraChunkPosition;

    //vars used to reduce lag caused by updating all islands meshes or/and colliders in one frame
    //work is splited into several frames and lag is almost invisible (depends on device)
    //tune these vars to achieve wanted performance (basically to reduce lag spike set values of CanUpdate... to higher)
    private float LastTimeMeshUpdate = -1;
    private const float CanUpdateMeshAfter = 0.1f;

    private float LastTimePartColliderUpdate = -1;
    private const float CanUpdateChunkPartColliderAfter = 0.1f;

    [HideInInspector]
    public bool CanUpdatePartCollider;

    public int IslandsAlive; //current islands in memory count

    // Use this for initialization
    void Start()
    {
        Islands = new List<Island>();

        CurrentCameraChunkPosition = new Vector2Int();
        LastCameraChunkPosition = new Vector2Int(-10000, -10000);

        if(OceanGenerator == null) {
            Debug.LogError("Ocean generator not found, critical error!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        CurrentCameraChunkPosition.x = Mathf.CeilToInt((WorldCamera.transform.position.x - OneGridPointToWorld / 2f) / OneGridPointToWorld);
        CurrentCameraChunkPosition.y = Mathf.CeilToInt((WorldCamera.transform.position.z - OneGridPointToWorld / 2f) / OneGridPointToWorld);

        if (!CurrentCameraChunkPosition.Equals(LastCameraChunkPosition))
        {
            //remove old chunks
            List<Island> ToRemove = new List<Island>();
            foreach (Island island in Islands)
            {
                int RangeToChunk = (int) Vector2Int.Distance(CurrentCameraChunkPosition, island.WorldGridPosition);

                if (RangeToChunk > DeleteDistance)
                {
                    ToRemove.Add(island);
                }
            }

            foreach (Island island in ToRemove)
            {
                Islands.Remove(island);
                Destroy(island.gameObject);
            }

            //try to generate new parts of ocean
            for (int x = -Range; x < Range; x++)
            {
                for (int z = -Range; z < Range; z++)
                {
                    int RangeToChunk = (int)Vector2Int.Distance(CurrentCameraChunkPosition, new Vector2Int(x + CurrentCameraChunkPosition.x, z + CurrentCameraChunkPosition.y));
                    if (RangeToChunk > Range)
                        continue;

                    int GridX = x + CurrentCameraChunkPosition.x;
                    int GridY = z + CurrentCameraChunkPosition.y;

                    if(IsIsland(GridX, GridY)) {
                        if (!IslandExists(GridX, GridY)) {
                            CreateIsland(GridX, GridY);
                        }
                    }
                }

            }
        }

        LastCameraChunkPosition.Set(CurrentCameraChunkPosition.x, CurrentCameraChunkPosition.y);

		if(Time.time - LastTimeMeshUpdate > CanUpdateMeshAfter) {
			Island chunkToUpdate = null;
			float curDst = 1000000f;
			foreach(Island chunk in Islands) {
				if(chunk.CreateMeshRequest) {
					float dst = Vector3.Distance(WorldCamera.transform.position, chunk.transform.position);
					if(dst < curDst) {
						curDst = dst;	
						chunkToUpdate = chunk;
					}
				}
			}

			if(chunkToUpdate != null) {
				chunkToUpdate.MakeMesh();
				chunkToUpdate.CreateMeshRequest = false;
			}

			LastTimeMeshUpdate = Time.time;
		}

        if(Time.time - LastTimePartColliderUpdate > CanUpdateChunkPartColliderAfter) {
            CanUpdatePartCollider = true;
            LastTimePartColliderUpdate = Time.time;
        }

        IslandsAlive = Islands.Count;
    }

    private bool IsIsland(int GridX, int GridY) {
        return OceanGenerator.IsIsland(GridX, GridY, this);
    }

    private bool IslandExists(int x, int z)
    {
        foreach (Island chunk in Islands)
        {
            if (chunk.WorldGridPosition.x == x && chunk.WorldGridPosition.y == z)
                return true;
        }
        return false;
    }

    private Island GetIsland(int x, int z)
    {
        foreach (Island chunk in Islands)
        {
            if (chunk.WorldGridPosition.x == x && chunk.WorldGridPosition.y == z)
                return chunk;
        }
        return null;
    }

    private void CreateIsland(int x, int z)
    {
        lock (ThreadLock)
        {
            Debug.Log("Creating new island x: " + x + " z: " + z);

            Island island;

            GameObject chunk = new GameObject("Island: " + x + "-" + 0 + "-" + z);
            chunk.transform.parent = transform;

            island = chunk.AddComponent<Island>();
            island.material = material;

            island.Init(this, new Vector2Int(x, z), OceanGenerator.GetGeneratorForIsland(x, z));
            Islands.Add(island);
        }
    }
}
