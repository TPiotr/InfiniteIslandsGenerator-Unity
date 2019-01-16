using UnityEngine;

/**
Generator interface, if you want custom generator just create new sciprt which implements this interface
 */
public interface IslandGenerator {

    int GetSeed();

    int GetPerIslandSeed(Island island);

    void SetupIslandForGeneration(Island island);

    float GetTerrainHeight(float x, float y, Island island);

    Color GetTerrainColor(float x, float y, float height, Island island);
}