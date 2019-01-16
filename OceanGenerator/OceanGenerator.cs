using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanGenerator : MonoBehaviour {

	public int Seed = 0;
	public MonoBehaviour islandGenerator;

	public float OceanScale = .001f;
	public float OceanThreshold = .7f;

	private FastNoise noise;

	// Use this for initialization
	void Start () {
		noise = new FastNoise(Seed);
		noise.SetNoiseType(FastNoise.NoiseType.Simplex);

		if(islandGenerator == null) {
			Debug.LogError("Island generator not found!, critical error!");
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public bool IsIsland(int GridX, int GridY, OceanManager manager) {
		float noiseFactor = noise.GetSimplex((GridX + 50) * OceanScale, (GridY + 13) * OceanScale) + 0.5f;
		
		if(noiseFactor > OceanThreshold) {
			//check if it is not going to collide with other islands
			foreach(Island island in manager.Islands) {
				float dst = Vector2.Distance(new Vector2(GridX, GridY), island.WorldGridPosition);

				if(dst * manager.OneGridPointToWorld < island.Size * 2) {
					return false;
				}
			}

			return true;
		}

		return false;
	}

	public IslandGenerator GetGeneratorForIsland(int GridX, int GridY) {
		//over there I'am using only one type of generator but you can f.e. use different generators for different parts of world/biomes etc. 
		return (IslandGenerator) islandGenerator;
	}
}
