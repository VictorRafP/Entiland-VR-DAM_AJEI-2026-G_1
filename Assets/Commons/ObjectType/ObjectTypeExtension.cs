using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectTypeExtension
{
    public static bool HaveMach(this List<ObjectType> l1, List<ObjectType> l2)
    {
        foreach (ObjectType objectType in l2)
        {
            if(l1.Contains(objectType))
            {
                return true;
            }
        }

        return false;
    }
}
