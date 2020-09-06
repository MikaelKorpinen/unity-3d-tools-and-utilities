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
        Debug.Log("Starting  " + startDelay);
        while (startDelay > 0)
        {
            startDelay -= Time.deltaTime * 0.1f;

            yield return new WaitForSeconds(Time.deltaTime * 0.1f);
        }
        var instantiatedAsset = GameObject.Instantiate(asset);
        if (parent)
        {
            instantiatedAsset.transform.parent = parent;
        }

        Debug.Log("Finished");
    }

    internal static IEnumerator TimedSpawnDeSpawnService(GameObject asset, float startDelay, float duration, Transform parent, string name)
    {

        GameObject spawnedObject = null;
        
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
        return Observable.FromCoroutine<GameObject>((observer, cancellationToken) => TimedSpawnService(asset, observer, delay));
    }
    private static IEnumerator TimedSpawnService(GameObject asset, IObserver<GameObject> observer, float delay)
    {
        while (delay > 0)
        {
            delay -= Time.deltaTime * 0.1f;
            yield return new WaitForSeconds(Time.deltaTime * 0.1f);
        }
        observer.OnNext(GameObject.Instantiate(asset));
        observer.OnCompleted();
    }
    private static IEnumerator TimedDeSpawnService(GameObject asset, float duration)
    {
        while (duration > 0)
        {
            duration -= Time.deltaTime * 0.1f;
            yield return new WaitForSeconds(Time.deltaTime * 0.1f);
        }
        GameObject.Destroy(asset);
    }
    
    private static IEnumerator TimedActionService(Action<GameObject, Transform, Transform> actionToPerform, GameObject asset, Transform source, Transform target, float startDelay,
        float duration)
    {
        while (duration > 0)
        {
            actionToPerform(asset, source, target);
            duration -= Time.deltaTime * 0.1f;
            yield return new WaitForSeconds(Time.deltaTime * 0.1f);
        }
    }
}
