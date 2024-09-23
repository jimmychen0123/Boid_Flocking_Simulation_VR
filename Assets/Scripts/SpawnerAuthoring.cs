using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

struct Spawner : IComponentData
{
    public Entity BoidPrefab;
    public int NumberOfBoids;
}
public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject BoidPrefab;
    public int NumberOfBoids;
    

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            /*
             * Because the spawner entity won’t be visible, it doesn’t need any Transform components, so TransformUsageFlags.None is specified
             */
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            var spawner = new Spawner
            {
                BoidPrefab = GetEntity(authoring.BoidPrefab, TransformUsageFlags.Dynamic),
                NumberOfBoids = authoring.NumberOfBoids,
            };

            AddComponent(entity, spawner);
        }
    }
}
