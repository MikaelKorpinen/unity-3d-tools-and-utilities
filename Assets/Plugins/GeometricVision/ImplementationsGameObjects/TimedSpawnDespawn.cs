using System;
using System.Collections;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.ImplementationsGameObjects
{
    public static class TimedSpawnDespawn
    {
        
        /// <summary>
        /// Coroutine that can spawn and destroys spawned asset based on delay and duration.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="startDelay">how long to wait until spawn</param>
        /// <param name="duration">How long the asset stays alive</param>
        /// <param name="parent">if not null parent spawned asset to this</param>
        /// <param name="name">name for spawned object</param>
        /// <returns></returns>
        internal static IEnumerator TimedSpawnDeSpawnService(GameObject asset, float startDelay, float duration,
            Transform parent, string name)
        {
            GameObject spawnedObject;

            Spawn(asset, startDelay).Subscribe(instantiatedAsset =>
            {
                spawnedObject = instantiatedAsset;
                instantiatedAsset.name = name;
                spawnedObject.name = name;
                if (parent)
                {
                    spawnedObject.transform.parent = parent;
                }

                Observable.FromCoroutine(x => TimedDeSpawnService(spawnedObject, duration)).Subscribe();
            });


            yield return null;
        }
        /// <summary>
        /// Coroutin that returns a gameObject
        /// </summary>
        /// <param name="asset">Asset to spawn</param>
        /// <param name="delay">Amount to delay spawn in seconds</param>
        /// <returns></returns>
        public static IObservable<GameObject> Spawn(GameObject asset, float delay)
        {
            // convert coroutine to IObservable
            return Observable.FromCoroutine<GameObject>((observer, cancellationToken) =>
                TimedSpawnService(asset, observer, delay));
        
            IEnumerator TimedSpawnService(GameObject assetIn, IObserver<GameObject> observer, float delayIn)
            {
                while (delayIn > 0)
                {
                    var countedTimeScale = Time.deltaTime;
                    delayIn -= countedTimeScale;
                    yield return null;
                }

                observer.OnNext(Object.Instantiate(assetIn));
                observer.OnCompleted();
            }
        }

    
        private static IEnumerator TimedDeSpawnService(GameObject asset, float duration)
        {
            
            while (duration > 0)
            {
                var countedTimeScale = Time.deltaTime;
                duration -= countedTimeScale;
                yield return null;
            }

            if (asset != null)
            {
                Object.Destroy(asset);
            }

        }

        private static IEnumerator TimedActionService(Action<GameObject, Transform, Transform> actionToPerform,
            GameObject asset, Transform source, Transform target, 
            float duration)
        {
            //Handle game paused and prevent division by zero problem by preventing the routine from proceeding if timescale is zero
            while (duration > 0)
            {
                var countedTimeScale = Time.deltaTime;
                actionToPerform(asset, source, target);
                duration -= countedTimeScale;
                yield return null;
            }
        }
    }
}