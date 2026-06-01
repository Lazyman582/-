using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum ActionIgnoreTag { Move, Attack, Dodge, Jump, Interact,crouch,die,Damage, All = 252 }

public struct ActionIgnoreMask
{
    private int _maskValue;

    public static ActionIgnoreMask GetMask(params ActionIgnoreTag[] tags)
    {
        var mask = new ActionIgnoreMask();

        foreach (var tag in tags)
        {
            if (tag == ActionIgnoreTag.All)
            {
                mask._maskValue = (int)ActionIgnoreTag.All;
                return mask;
            }
            mask._maskValue |= 1 << (int)tag;
        }
        return mask;
    }

    public bool ContainTag(ActionIgnoreTag tag)
    {
        return (_maskValue & (1 << (int)tag)) != 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ActionIgnoreMask mask && _maskValue == mask._maskValue;
    }
}

public class ActionIgnore
{
    public ActionIgnoreMask Mask;
    public float timer;

    public ActionIgnore(ActionIgnoreMask mask, float time)
    {
        Mask = mask;
        timer = time;
    }
}