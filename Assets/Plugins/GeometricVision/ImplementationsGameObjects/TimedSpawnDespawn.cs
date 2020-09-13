using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using UniRx;
using UnityEngine;

public static class TimedSpawnDespawn
{
    public static IEnumerator TimedInstantiateService(GameObject asset, float startDelay, Transform parent)
    {
        //Handle game paused and prevent division by zero problem by preventing the routine from proceeding if timescale is zero
        while (Time.timeScale == 0)
        {
            yield return null;
        }
        
        while (startDelay > 0)
        {
            var countedTimeScale = Time.deltaTime /  Time.timeScale;
            startDelay -= countedTimeScale;

            yield return new WaitForSeconds(countedTimeScale);
        }

        var instantiatedAsset = GameObject.Instantiate(asset);
        if (parent)
        {
            instantiatedAsset.transform.parent = parent;
        }

        Debug.Log("Finished");
    }

    internal static IEnumerator TimedSpawnDeSpawnService(GameObject asset, float startDelay, float duration,
        Transform parent, string name)
    {
        //Handle game paused and prevent division by zero problem by preventing the coroutine from proceeding if timescale is zero
        while (Time.timeScale == 0)
        {
            yield return null;
        }
        if (asset == null)
        {
           yield break;
        }

        GameObject spawnedObject;

        Spawn(asset, startDelay).Subscribe(instantiatedAsset =>
        {
            spawnedObject = instantiatedAsset;
            spawnedObject.name = name;
            if (parent)
            {
                spawnedObject.transform.parent = parent;
            }

            Observable.FromCoroutine(x => TimedDeSpawnService(spawnedObject, duration)).Subscribe();
        });


        yield return null;
    }

    public static IObservable<GameObject> Spawn(GameObject asset, float delay)
    {
        // convert coroutine to IObservable
        return Observable.FromCoroutine<GameObject>((observer, cancellationToken) =>
            TimedSpawnService(asset, observer, delay));
        
        IEnumerator TimedSpawnService(GameObject assetIn, IObserver<GameObject> observer, float delayIn)
        {
            //Handle game paused and prevent division by zero problem by preventing the routine from proceeding if timescale is zero
            while (Time.timeScale == 0f)
            {
                yield return null;
            }
            while (delayIn > 0)
            {
                var countedTimeScale = Time.deltaTime /  Time.timeScale;
                delayIn -= countedTimeScale;
                yield return new WaitForSeconds(countedTimeScale);
            }

            observer.OnNext(GameObject.Instantiate(assetIn));
            observer.OnCompleted();
        }
    }

    
    private static IEnumerator TimedDeSpawnService(GameObject asset, float duration)
    {
        //Handle game paused and prevent division by zero problem by preventing the routine from proceeding if timescale is zero
        while (Time.timeScale == 0)
        {
            yield return null;
        }
        while (duration > 0)
        {
            var countedTimeScale = Time.deltaTime /  Time.timeScale;
            duration -= countedTimeScale;
            yield return new WaitForSeconds(countedTimeScale);
        }

        if (asset != null)
        {
            GameObject.Destroy(asset);
        }

    }

    private static IEnumerator TimedActionService(Action<GameObject, Transform, Transform> actionToPerform,
        GameObject asset, Transform source, Transform target, 
        float duration)
    {
        //Handle game paused and prevent division by zero problem by preventing the routine from proceeding if timescale is zero
        while (Time.timeScale == 0)
        {
            yield return null;
        }
        while (duration > 0)
        {
            var countedTimeScale = Time.deltaTime /  Time.timeScale;
            actionToPerform(asset, source, target);
            duration -= countedTimeScale;
            yield return new WaitForSeconds(countedTimeScale);
        }
    }
}