using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        /*
         * Systems are usually instantiated and start updating even before the initial scene has loaded, yet here you don’t want this system to update until the entity with the Spawner component has been loaded from the subscene. By calling the SystemState method RequireForUpdate<Spawner>() in OnCreate, the system won’t update in a frame unless at least one entity with the Spawner component currently exists.
         */

        state.RequireForUpdate<Spawner>();
        state.RequireForUpdate<Config>();
 
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
         * The SpawnSystem is a system that you only want to update once, so in the OnUpdate, the Enabled property of the SystemState is set to false, which prevents subsequent updates of the system.  
         */
        state.Enabled = false;

        var prefab = SystemAPI.GetSingleton<Spawner>().BoidPrefab;
        int totalBoids = SystemAPI.GetSingleton<Spawner>().NumberOfBoids;
        var instances = state.EntityManager.Instantiate(prefab, totalBoids, Allocator.Temp);
        var config = SystemAPI.GetSingleton<Config>();
        float3 area = config.Area;
        
        // randomly set the positions of the new cubes
        // either using a fixed seed, such as 123 
        // for different randomness in each run, use the elapsed time value as the seed)
        var random = new Random(123);
        foreach (var entity in instances)
        {
            var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
            transform.ValueRW.Position = random.NextFloat3(-area, area);
            SystemAPI.GetComponentRW<BoidData>(entity).ValueRW.speed = random.NextFloat(config.MinSpeed, config.MaxSpeed);
        }
    }
}
