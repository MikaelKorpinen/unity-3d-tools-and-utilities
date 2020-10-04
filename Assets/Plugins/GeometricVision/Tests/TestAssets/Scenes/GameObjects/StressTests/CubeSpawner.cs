using UnityEngine;

namespace Plugins.GeometricVision.Tests.TestAssets.Scenes.GameObjects.StressTests
{
    public class CubeSpawner : MonoBehaviour
    {
        [SerializeField] private float cubesPerRow;
        [SerializeField] private float cubesPerColumn;
        [SerializeField] private float distanceMultiplier;
        [SerializeField] private GameObject modelToSpawn;
    
        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < cubesPerRow; i++)
            {
                for (int j = 0; j < cubesPerColumn; j++)
                {
                    Instantiate(modelToSpawn);
                    modelToSpawn.transform.position = new Vector3(i * distanceMultiplier, j * distanceMultiplier, 0f); 
                }
            }
        }

    }
}
