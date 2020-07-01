using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces;
using UnityEngine;

public static class InterfaceUtilities 
{
    public static bool ListContainsInterfaceOfType<T>(Type typeToCheck, List<T> interfaces)
    {
        bool found = false;

        foreach (var processor in interfaces)
        {

            if ( processor.GetType() == typeToCheck)
            {
                found = true;
            }
        }

        return found;
    }
    
    public static void RemoveInterfacesOfTypeFromList<T>(Type typeToCheck, ref List<T> interfaces)
    {
        List<T> tempList = new List<T>(interfaces);

        foreach (var processor in tempList)
        {

            if ( processor.GetType() == typeToCheck)
            {
                interfaces.Remove(processor);
            }
        }

        interfaces = tempList;
    }
    
    public static T GetInterfaceOfTypeFromList<T>(Type typeToCheck, List<T> interfaces)
    {
        T interfaceToReturn = default(T);
        foreach (var processor in interfaces)
        {

            if ( processor.GetType() == typeToCheck)
            {
                interfaceToReturn = processor;
            }
        }

        return interfaceToReturn;
    }
}
