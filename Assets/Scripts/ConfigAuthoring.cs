using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct Config : IComponentData
{
    public float3 Area;
 
    public float MinSpeed;
   
    public float MaxSpeed;

}
public class ConfigAuthoring : MonoBehaviour
{
    public float3 Area;
    
    [Range(0.0f, 5.0f)]
    public float MinSpeed;

    [Range(0.0f, 5.0f)]
    public float MaxSpeed;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            /*
             * Because the config entity won’t be visible, it doesn’t need any Transform components, so TransformUsageFlags.None is specified
             */
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            var config = new Config 
            {
                Area = authoring.Area,
                MinSpeed = authoring.MinSpeed,
                MaxSpeed = authoring.MaxSpeed,
            };

            AddComponent(entity, config);
        }

    }

}
