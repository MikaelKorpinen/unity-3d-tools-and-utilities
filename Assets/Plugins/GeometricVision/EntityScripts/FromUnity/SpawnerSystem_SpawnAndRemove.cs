using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

// Systems can schedule work to run on worker threads.
// However, creating and removing Entities can only be done on the main thread to prevent race conditions.
// The system uses an EntityCommandBuffer to defer tasks that can't be done inside the Job.

// ReSharper disable once InconsistentNaming
namespace Plugins.GeometricVision.EntityScripts.FromUnity
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class SpawnerSystem_SpawnAndRemove : SystemBase
    {
        // BeginInitializationEntityCommandBufferSystem is used to create a command buffer which will then be played back
        // when that barrier system executes.
        //
        // Though the instantiation command is recorded in the SpawnJob, it's not actually processed (or "played back")
        // until the corresponding EntityCommandBufferSystem is updated. To ensure that the transform system has a chance
        // to run on the newly-spawned entities before they're rendered for the first time, the SpawnerSystem_FromEntity
        // will use the BeginSimulationEntityCommandBufferSystem to play back its commands. This introduces a one-frame lag
        // between recording the commands and instantiating the entities, but in practice this is usually not noticeable.
        //
        BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        private bool runOnce;
        protected override void OnCreate()
        {
            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            runOnce = true;
        }

        protected override void OnUpdate()
        {
            // Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to
            // perform such changes on the main thread after the Job has finished. Command buffers allow you to perform
            // any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and
            // deletions for later.
            var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            if (runOnce)
            {
                // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
                // Since this job only runs on the first frame, we want to ensure Burst compiles it before running to get the best performance (3rd parameter of WithBurst)
                // The actual job will be cached once it is compiled (it will only get Burst compiled once).
                Entities
                    .ForEach((Entity entity, int entityInQueryIndex, in SpawnerData_SpawnAndRemove spawner,
                        in LocalToWorld location) =>
                    {
                        var random = new Random(1);

                        for (var x = 0; x < spawner.CountX; x++)
                        {
                            for (var y = 0; y < spawner.CountY; y++)
                            {
                                var instance = commandBuffer.Instantiate(entityInQueryIndex, spawner.Prefab);
                                var separationMultiplier = spawner.separationMultiplier;
                                // Place the instantiated in a grid with some noise
                                var position = (Vector3) math.transform(location.Value,
                                    new float3(x * 1.3F - spawner.CountX * separationMultiplier, y * 1.3F * separationMultiplier, noise.cnoise(new float2(x, y) * 0.21F) * 2 + 25) * separationMultiplier);
                                commandBuffer.SetComponent(entityInQueryIndex, instance, new Translation {Value = position});
                                commandBuffer.AddComponent(entityInQueryIndex, instance, new RotationSpeed_SpawnAndRemove { RadiansPerSecond = math.radians(random.NextFloat(25.0F, 90.0F)*25) });
                            }
                        }

                       // commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }).ScheduleParallel();
                runOnce = false;
            }
            var deltaTime = Time.DeltaTime;
        
            // The in keyword on the RotationSpeed_SpawnAndRemove component tells the job scheduler that this job will not write to rotSpeedSpawnAndRemove
            Entities
                .WithName("RotationSpeedSystem_SpawnAndRemove")
                .ForEach((ref Rotation rotation, in RotationSpeed_SpawnAndRemove rotSpeedSpawnAndRemove) =>
                {
                    // Rotate something about its up vector at the speed given by RotationSpeed_SpawnAndRemove.
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeedSpawnAndRemove.RadiansPerSecond * deltaTime*0.01f));
                }).ScheduleParallel();
            // SpawnJob runs in parallel with no sync point until the barrier system executes.
            // When the barrier system executes we want to complete the SpawnJob and then play back the commands
            // (Creating the entities and placing them). We need to tell the barrier system which job it needs to
            // complete before it can play back the commands.
            m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}