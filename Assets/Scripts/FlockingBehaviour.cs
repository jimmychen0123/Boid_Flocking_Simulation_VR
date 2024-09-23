
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class FlockingBehaviour : MonoBehaviour
{
    [SerializeField] BoidConfigSO _boidConfigSO;
    [SerializeField] Mesh _mesh;
    [SerializeField] Material _material;

    [Header("Flocking settings")]
    [Range(200, 100000)] [SerializeField] int _maxPopulation;
    [Range(0.1f, 1f)] [SerializeField] float _moveSpeed;
    [Range(15, 360)] [Tooltip("Rotation in Degree")] [SerializeField] float _turnSpeed;
    [SerializeField, Min(1)] int _searchRange;
    [Range(180f, 360f)] [SerializeField] float2 _repelAngle;
    [SerializeField][Tooltip("How far the boids can go in meters")] float _schoolRadius;
    [Range(0.005f, 0.5f)][SerializeField] float _agility;

    [Tooltip("Contains Boid's Transformation data, used in MonoBehaviour")]
    List<Matrix4x4> _listBoidsTRS;
    [Tooltip("Contains Boid's Transformation data, used in Job System")]
    NativeList<Matrix4x4> _NativeListBoidsTRS;

    JobHandle handle;
    Unity.Mathematics.Random _random;
    float timeSinceLastTick;

    [Tooltip("ensures that boid position updates happen at approximately tickDelay intervals")]
    float tickDelay = 0.015f;

    public int MaxPopulation 
    { 
        get => _maxPopulation; 
        set 
        {
            _maxPopulation = Mathf.Clamp(value, 200, 100000);
            _boidConfigSO.MaxPopulation = _maxPopulation;
        }
    }

    private void Awake()
    {
        _maxPopulation = _boidConfigSO.MaxPopulation;
        _listBoidsTRS = new List<Matrix4x4>();
        _NativeListBoidsTRS = new NativeList<Matrix4x4>(1, Allocator.Persistent);
        _random = new Unity.Mathematics.Random(1);

        for (int i = 0; i < _maxPopulation; i++)
        {
            AddBoid(Vector3.zero, Quaternion.identity, 1);
        }
    }

    void Update()
    {
        // Draw boids using instanced rendering
        if (_listBoidsTRS.Count > 0)
        {
            RenderParams renderParameters = new RenderParams(_material);
            Graphics.RenderMeshInstanced(renderParameters, _mesh, 0, _listBoidsTRS);
        }

        // Manage timing for job updates based on tick delay
        timeSinceLastTick += Time.deltaTime;
        if (timeSinceLastTick >= tickDelay)
        {
            timeSinceLastTick = 0;
            UpdateBoidsData();
            ScheduleJob();
            handle.Complete(); // Ensure job is complete before updating the render list
            UpdateRenderList();
        }
    }

    private void OnDestroy()
    {
        handle.Complete();
        if (_NativeListBoidsTRS.IsCreated)
        {
            _NativeListBoidsTRS.Dispose();
        }
    }

    void AddBoid(Vector3 pos, Quaternion rotation, float scale)
    {
        _listBoidsTRS.Add(Matrix4x4.TRS(pos, rotation, Vector3.one * scale));
    }
    /// <summary>
    ///Updates the transformation data of the boids, transferring the data from the _listBoidsTRS to the _NativeListBoidsTRS, which is required for the job system to process. 
    ///Data Transfer: It's an easy way to bridge the gap between managed (List) and unmanaged (NativeList) memory
    /// </summary>
    private void UpdateBoidsData()
    {
        _NativeListBoidsTRS.SetCapacity(_listBoidsTRS.Count);
        NativeArray<Matrix4x4> temp = new NativeArray<Matrix4x4>(_listBoidsTRS.ToArray(), Allocator.TempJob);
        _NativeListBoidsTRS.CopyFrom(temp);
        temp.Dispose();
    }
    /// <summary>
    /// Schedules the job responsible for updating the boids' positions, orientations, and behaviors. This offloads the calculations to Unity's job system, leveraging multithreading for performance.
    /// </summary>
    private void ScheduleJob()
    {
        UpdateJob job = new UpdateJob
        {
            DeltaTime = Time.deltaTime,
            MoveSpeed = _moveSpeed,
            BoidsTRS = _NativeListBoidsTRS,
            Random = _random,
            SearchRange = _searchRange,
            RepelAngle = _repelAngle,
            Center = transform.position,
            SchoolRadius = _schoolRadius,
            SchoolUp = transform.up,
            Agility = _agility,
        };
        // https://docs.unity3d.com/Manual/JobSystemCreatingJobs.html
        // https://docs.unity.cn/2023.3/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html#:~:text=Unity%20automatically%20splits%20the%20work%20into%20chunks%20no,the%20amount%20of%20work%20performed%20in%20the%20job.
        handle = job.Schedule(_listBoidsTRS.Count, 2);
    }
    /// <summary>
    /// Updates the render list with the new transformations calculated by the job. This ensures the visual representation of the boids matches their updated positions and orientations.
    /// </summary>
    private void UpdateRenderList()
    {
        Parallel.For(0, _listBoidsTRS.Count, (i) =>
        {
            _listBoidsTRS[i] = _NativeListBoidsTRS[i];
        });
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelFor
    {
        // Job data: contains boid transformation data, movement settings, and environment factors
        [NativeDisableParallelForRestriction] public NativeList<Matrix4x4> BoidsTRS; // List of boid transformation matrices
        [ReadOnly] public Unity.Mathematics.Random Random; // Random number generator for various calculations
        [ReadOnly] public float MoveSpeed; // Movement speed of the boids
        [ReadOnly] public float DeltaTime; // Time elapsed since the last update
        [ReadOnly] public int SearchRange; // Range to search for nearby boids
        [ReadOnly] public float2 RepelAngle; // Angles to repel boids from each other
        [ReadOnly] public float3 Center; // Center point of the flock
        [ReadOnly] public float SchoolRadius; // Radius of the flock's area
        [ReadOnly] public float3 SchoolUp; // Up direction of the school (flock)
        [ReadOnly] public float Agility; // How quickly boids can turn

        // Executes job for each boid (indexed by 'index')
        public void Execute(int index)
        {
            // Retrieve boid's transformation matrix and decompose it into position, rotation, and scale
            Matrix4x4 trs = BoidsTRS[index];
            float3 position = trs.GetPosition();
            Quaternion rotation = trs.rotation;
            Vector3 scale = trs.lossyScale;

            // Compute translation vectors based on boid's rotation
            float3 forwardTranslationMatrix = math.mul(rotation, new float3(0, 0, 1)); // Forward direction
            float3 upTranslationMatrix = math.mul(rotation, new float3(0, 1, 0)); // Up direction
            float3 rightTranslationMatrix = math.mul(rotation, new float3(1, 0, 0)); // Right direction

            // Move boid forward based on movement speed and delta time
            position += forwardTranslationMatrix * MoveSpeed * DeltaTime;

            #region Neighbor Search
            // Randomly select a range of neighbors to interact with
            int startIndex = Random.NextInt(0, BoidsTRS.Length);
            int endIndex = Random.NextInt(startIndex + 1, startIndex + 1 + SearchRange);
            endIndex = Mathf.Clamp(endIndex, 0, BoidsTRS.Length);

            // Variables to track repulsion angles and direction
            float totalRepelAngleX = 0;
            float totalRepelAngleY = 0;
            float3 repelDirection = 0;

            // Iterate through neighbors to calculate repulsion forces
            for (int i = startIndex; i < endIndex; i++)
            {
                Matrix4x4 neighborTRS = BoidsTRS[i]; // Get neighbor's transformation
                float3 neighborPos = neighborTRS.GetPosition(); // Get neighbor's position
                float distance = math.distance(neighborPos, position); // Calculate distance to neighbor

                if (distance < 0.02f)
                {
                    // Apply random repulsion when too close
                    totalRepelAngleX += RepelAngle.x * Random.NextFloat(-2, 2);
                    totalRepelAngleY += RepelAngle.y * Random.NextFloat(-2, 2);
                }
                else
                {
                    // Calculate repulsion based on distance using a special function
                    float3 fromNeighborToMeVec = position - neighborPos;
                    float d = math.length(fromNeighborToMeVec);
                    float multiplier = SpecialFunc(d);
                    repelDirection += (fromNeighborToMeVec / d) * multiplier; // Accumulate repulsion direction
                }
            }

            // Calculate the direction and distance of the boid relative to the center of the flock
            float3 meToCenter = Center - position;
            float disFromCenter = math.length(meToCenter);
            float3 meToCenterDir = meToCenter / disFromCenter;

            // Calculate a factor that drives the boid back to the flock's center
            float goHomeT = disFromCenter / SchoolRadius;
            #endregion

            #region Move Away from Neighbors
            // Calculate the target rotation considering repulsion from neighbors and attraction to the center
            Quaternion targetRot = Quaternion.LookRotation(repelDirection + meToCenter, SchoolUp) *
                                    Quaternion.AngleAxis(totalRepelAngleX, upTranslationMatrix) *
                                    Quaternion.AngleAxis(totalRepelAngleY, rightTranslationMatrix);

            // Interpolate towards the target rotation, with more influence from the center as the boid strays further
            targetRot = Quaternion.Lerp(
                                    targetRot,
                                    Quaternion.LookRotation(meToCenter, upTranslationMatrix),
                                    0.95f * goHomeT);

            // Gradually rotate the boid towards the target rotation based on its agility
            rotation = Quaternion.Lerp(rotation, targetRot, Agility);
            #endregion

            // Update the boid's transformation matrix with the new position and rotation
            BoidsTRS[index] = Matrix4x4.TRS(position, rotation, scale);
        }

        // A function to calculate a repulsion factor based on distance
        private float SpecialFunc(float d)
        {
            return 1 / (1 + d); // The closer the neighbor, the stronger the repulsion
        }
    }

}

