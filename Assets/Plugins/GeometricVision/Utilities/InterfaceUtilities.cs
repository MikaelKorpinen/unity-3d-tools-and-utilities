using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InterfaceUtilities
{
    public static bool ListContainsInterfaceImplementationOfType<T>(Type typeToCheck, List<T> interfaces)
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

    public static void RemoveInterfaceImplementationsOfTypeFromList<T>(Type typeToCheck, ref List<T> implementations)
    {

        List<T> tempList = new List<T>();
        foreach (var processor in implementations)
        {
            if (processor.GetType() != typeToCheck)
            {
                tempList.Add(processor);
            }
        }

        implementations = tempList;
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

    public static T GetInterfaceImplementationOfTypeFromList<T>(Type typeToCheck, List<T> implementations)
    {
        T interfaceToReturn = default(T);
        foreach (var processor in implementations)
        {
            if (processor.GetType() == typeToCheck)
            {
                interfaceToReturn = processor;
            }
        }

        return interfaceToReturn;
    }
    
    public static HashSet<T> GetInterfaceImplementationsOfTypeFromList<T>(Type typeToCheck, HashSet<T> implementations)
    {
        HashSet<T> toReturn = new HashSet<T>();
        foreach (var implementation in implementations)
        {
            if (implementation.GetType() == typeToCheck)
            {
                toReturn.Add(implementation);
            }
        }
        return toReturn;
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