using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public struct BoidData : IComponentData
{
    public float RadiansPerSecond;  // how quickly the entity rotates
    public float speed; 

}
public class BoidAuthoring : MonoBehaviour
{
    public float DegreesPerSecond = 360.0f;
    //public GameObject Config;

    class Baker : Baker<BoidAuthoring>
    {
        public override void Bake(BoidAuthoring authoring)
        {

            //DependsOn(authoring.Config);

            //if (authoring.Config == null) return;
            //var config = GetComponent<ConfigAuthoring>(authoring.Config);
            // retrieve the entity being baked and pass the enum value TransformUsageFlags.Dynamic to specify that the entity needs the standard transform components, including LocalTransform.
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var random = new Unity.Mathematics.Random(321);
            var boid = new BoidData
            {
                RadiansPerSecond = math.radians(authoring.DegreesPerSecond),
                //speed = random.NextFloat(config.MinSpeed, config.MaxSpeed),
            };

            AddComponent(entity, boid);
        }
    }
}
