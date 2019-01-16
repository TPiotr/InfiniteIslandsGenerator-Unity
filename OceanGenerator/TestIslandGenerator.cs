using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Generator example
 */
public class TestIslandGenerator : MonoBehaviour, IslandGenerator {

	public float WaterLevel = 6f; // height of water in worldspace coords
	public float TerrainNoiseScale = .003f;
	public float TerrainAmp = 10f;

	public Color BeachColor;
	public Color GrassColor;

	private FastNoise IslandNoise;

	private float noiseOffset;
	private bool circleShaped;

	private Random random;

	//per island unique properties
	private class PerIslandGeneratorProperties : IslandGeneratorProperties {
		public float noiseOffset;
		public int islandSize;
		public bool circleShaped;
	}

	// Use this for initialization
	void Start () {
		IslandNoise = new FastNoise(GetSeed());
		IslandNoise.SetNoiseType(FastNoise.NoiseType.Perlin);
        IslandNoise.SetFractalOctaves(4);
	}

	public void SetupIslandForGeneration(Island island) {
		//always use random with static unique per island seed so engine will generate always same island in given position
		random = new Random(GetPerIslandSeed(island));
		
		//height generation properties
		PerIslandGeneratorProperties properties = new PerIslandGeneratorProperties();
		island.GeneratorProperties = properties;

		properties.islandSize = (int) random.Range(300, 1000);
		properties.noiseOffset = random.Range(.4f, 1f);
		properties.circleShaped = random.Bool();

		//mesh generation properties
		island.Size = properties.islandSize;
		island.minPointRadius = 6;
		island.trianglesInChunk = 1500;
	}

	public int GetSeed() {
		return 1;
	}

	public float GetTerrainHeight(float x, float y, Island island) {
		//simple height  generation (only one noise pass)
		PerIslandGeneratorProperties properties = (PerIslandGeneratorProperties) island.GeneratorProperties;

		float height = IslandNoise.GetPerlin(x * TerrainNoiseScale, y * TerrainNoiseScale) + properties.noiseOffset;
		height *= TerrainAmp;

		//circular, square masks (to achieve island looking terrain)
		float distance_x = Mathf.Abs(x - island.Size * 0.5f);
		float distance_y = Mathf.Abs(y - island.Size * 0.5f);
		
		float distance = 0f;
		if(properties.circleShaped) {
			distance = Mathf.Sqrt(distance_x * distance_x + distance_y * distance_y); // circular mask
		} else {
			distance = Mathf.Max(distance_x, distance_y); // square mask
		}

		float max_width = island.Size * 0.5f - 10.0f;
		float delta = distance / max_width;
		float gradient = delta * delta;

		height *= Mathf.Max(0.0f, 1.0f - gradient);

		return height;
	}

	public Color GetTerrainColor(float x, float y, float height, Island island) {
		//create beach like effect with this piece of code, change smoothRange var value to define beach size
		Color oc = GrassColor;
		float smoothRange = 5f;
		if(height > WaterLevel && InRange(height, WaterLevel, smoothRange)) {
			float lerpFactor = (height - WaterLevel) / smoothRange;
			oc = Color.Lerp(BeachColor, GrassColor, lerpFactor);
		} else if(height < WaterLevel) {
			oc = BeachColor;
		}

		return oc; 
	}

	private bool InRange(float a, float b, float range) {
		return(Mathf.Abs(a - b) < range);
	}

	public int GetPerIslandSeed(Island island) {
		//get unique per island position id using pairing function
		int chunkSeed = (int) (1f/2f * (island.WorldGridPosition.x + island.WorldGridPosition.y) * (island.WorldGridPosition.x + island.WorldGridPosition.y + 1) 
			+ island.WorldGridPosition.y);

        chunkSeed += GetSeed();

		return chunkSeed;
	}
}
