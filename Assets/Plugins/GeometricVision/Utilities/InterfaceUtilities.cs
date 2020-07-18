using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;

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

    public static void RemoveInterfaceImplementationOfTypeFromList<T>(Type typeToCheck, ref List<T> implementations)
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

    public static void RemoveInterfaceImplementationOfTypeFromList<T>(Type typeToCheck, ref HashSet<T> implementations)
    {
        HashSet<T> tempList = new HashSet<T>(implementations);

        foreach (var processor in tempList)
        {
            if (processor.GetType() == typeToCheck)
            {
                implementations.Remove(processor);
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

    public static T GetInterfaceImplementationOfTypeFromList<T>(Type typeToCheck, HashSet<T> implementations)
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
}