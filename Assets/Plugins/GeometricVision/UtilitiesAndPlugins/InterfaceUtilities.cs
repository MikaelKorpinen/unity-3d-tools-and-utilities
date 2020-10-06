using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InterfaceUtilities
{

    public static bool ListContainsInterfaceImplementationOfType<T>(Type typeToCheck, HashSet<T> interfaces)
    {
        bool found = false;

        foreach (var processor in interfaces)
        {
            if (processor.GetType() == typeToCheck && processor != null)
            {
                found = true;
            }
        }

        return found;
    }
    

    public static void AddImplementation<Tinterface, TValue>(TValue implementationToAdd, HashSet<Tinterface> implementations) where TValue: Tinterface
    {
        if (implementations == null)
        {
            implementations = new HashSet<Tinterface>();
        }

        if (ListContainsInterfaceImplementationOfType(implementationToAdd.GetType(), implementations) == false)
        {
            var dT = (Tinterface) default(TValue);
            if (Equals(implementationToAdd, dT) == false)
            {
                implementations.Add(implementationToAdd);
            }
        }
    }
    public static void AddImplementation<TInterface, TImplementation>(Func<IGeoEye> action, HashSet<TInterface> implementations, GameObject gameObject) where TImplementation: TInterface
    {
        if (implementations == null)
        {
            implementations = new HashSet<TInterface>();
        }
        var eye = gameObject.GetComponent(typeof(TImplementation));
        if (eye == null)
        {
            gameObject.AddComponent(typeof(TImplementation));
        }

        TInterface implementation = (TInterface)action.Invoke();
        if (ListContainsInterfaceImplementationOfType(implementation.GetType(), implementations) == false)
        {
           
            var dT = (TInterface) default(TImplementation);
            if (Equals(implementation, dT) == false)
            {
                implementations.Add(implementation);
            }
        }
    }
    public static void RemoveInterfaceImplementationsOfTypeFromList<T>(Type typeToCheck, ref HashSet<T> implementations)
    {
        HashSet<T> tempList = new HashSet<T>();

        foreach (var implementation in implementations)
        {
            if (implementation.GetType() != typeToCheck)
            {
                tempList.Add(implementation);
            }
        }

        implementations = tempList;
    }

    public static T GetInterfaceImplementationOfTypeFromList<T>(Type typeToCheck, HashSet<T> implementations)
    {
        T interfaceToReturn = default(T);
        foreach (var implementation in implementations)
        {
            if (implementation.GetType() == typeToCheck)
            {
                interfaceToReturn = implementation;
                break;
            }
        }

        return interfaceToReturn;
    }
}