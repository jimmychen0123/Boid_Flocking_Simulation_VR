using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
partial struct FlockingJob : IJobEntity
{
    public float DeltaTime;

    // ref: read write access 
    // in: read-only access
    void Execute(ref LocalTransform transform, in BoidData flockingData)
    {
        transform = transform.RotateY(flockingData.RadiansPerSecond * DeltaTime);
        transform = transform.Translate(new float3(0,1,0) * flockingData.speed * DeltaTime);
    }
}
public partial struct FlockingSystem : ISystem
{
    /*
     * Do not assume that the main scene has loaded before the method called
     */
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // put a condition on system update, the system will not updatet unless there exists at least one entity in the World having this execute.main thread component  
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var job = new FlockingJob { DeltaTime = SystemAPI.Time.DeltaTime };
        job.Schedule();
        
        ///*
        // * Reasons not using Unity.Time class:
        // * 1. relating to netcode, the world that the system belong to maintains its own time value.
        // */ 
        //var deltaTime = SystemAPI.Time.DeltaTime;

        //// This foreach loops through all entities that have LocalTransform and RotationSpeed components. 
        //// You need to modify the LocalTransform, so it is wrapped in RefRW (read-write). 
        //// You only need to read the RotationSpeed, so it is wrapped in RefRO (read only). 
        //foreach (var (transform, rotationSpeed) in
        //        SystemAPI.Query<RefRW<LocalTransform>, RefRO<FlockingData>>())
        //{
        //    // Rotate the transform around the Y axis. 
        //    var radians = rotationSpeed.ValueRO.RadiansPerSecond * deltaTime;
        //    transform.ValueRW = transform.ValueRW.RotateY(radians);
        //}
    }
}

