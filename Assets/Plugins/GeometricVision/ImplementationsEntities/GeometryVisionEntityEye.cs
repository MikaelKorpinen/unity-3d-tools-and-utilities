using System;
using System.Collections.Generic;
using Plugins.GeometricVision.Debugging;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsEntities
{
    [AlwaysUpdateSystem]
    [DisableAutoCreation]
    [UpdateBefore(typeof(GeometryVisionEntityProcessor))]
    public class GeometryVisionEntityEye : SystemBase, IGeoEye
    {
        public string Id { get; set; }
        public GeometryVisionRunner Runner { get; set; }

        
        public GeometryVision GeoVision { get; set; }
        private EyeDebugger _debugger;
        private bool _addedByFactory;

        [SerializeField, Tooltip(" Geometry is extracted from collider instead of renderers mesh")]
        private bool targetColliderMeshes;

        private List<TargetingInstruction> targetingInstructions = new List<TargetingInstruction>();
        private int lastCount;
        [System.ComponentModel.ReadOnly(true)] private EntityManager entityManager;
        private BeginInitializationEntityCommandBufferSystem entityCommandBuffer;


        protected override void OnCreate()
        {
            entityManager = EntityManager;
            entityCommandBuffer = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            Debugger = new EyeDebugger();
        }


        /// <summary>
        /// Here is handled object visibility processing and filtering
        /// </summary>
        protected override void OnUpdate()
        {
            var entityQuery = GetEntityQuery(typeof(GeometryDataModels.Target));
            var planes = new NativeArray<Plane>(6, Allocator.TempJob);

            planes.CopyFrom(GeoVision.Planes);
            var job2 = new CheckVisibility()
            {
                targets = entityQuery.ToComponentDataArray<GeometryDataModels.Target>(Allocator.TempJob),

                planes = new NativeArray<Plane>(6, Allocator.TempJob),
                entities = entityQuery.ToEntityArray(Allocator.TempJob)
            };
            job2.planes.CopyFrom(planes);
            Dependency = job2.Schedule(job2.targets.Length, 6);
            Dependency.Complete();
            //Wait for job completion

           
            entityCommandBuffer.AddJobHandleForProducer(Dependency);
            entityQuery.CopyFromComponentDataArray<GeometryDataModels.Target>(job2.targets);

            lastCount = entityQuery.CalculateEntityCount();
            job2.planes.Dispose();
            job2.entities.Dispose();
            job2.targets.Dispose();
            planes.Dispose();

        }

        [BurstCompile]
        public struct CheckVisibility : IJobParallelFor
        {
            public NativeArray<GeometryDataModels.Target> targets;

            [System.ComponentModel.ReadOnly(true)] [NativeDisableParallelForRestriction]
            public NativeArray<Plane> planes;
            public NativeArray<Entity> entities;

            public void Execute(int index)
            {
                GeometryDataModels.Target target = targets[index];
                target.isSeen = IsInsideFrustum(target.position, planes);
                targets[index] = target;
            }
        }
        

        public static bool IsInsideFrustum(GeometryDataModels.Edge edge, NativeArray<Plane> planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                if (planes[index].GetDistanceToPoint(edge.firstVertex) < 0 ||
                    planes[index].GetDistanceToPoint(edge.secondVertex) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInsideFrustum(Vector3 point, NativeArray<Plane> planes)
        {
            for (var index = 0; index < planes.Length; index++)
            {
                var plane = planes[index];
                if (plane.GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }


        ///<inheritdoc cref="IGeoEye"/>
        /// <param name="useBounds"></param>
        public void UpdateVisibility(bool useBounds)
        {
            this.Update();
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }

        public List<TargetingInstruction> TargetingInstructions
        {
            get { return targetingInstructions; }
            set { targetingInstructions = value; }
        }

        public EyeDebugger Debugger
        {
            get { return _debugger; }
            set { _debugger = value; }
        }

        public bool TargetColliderMeshes
        {
            get { return targetColliderMeshes; }
            set { targetColliderMeshes = value; }
        }
    }
}