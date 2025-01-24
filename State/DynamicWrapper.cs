using System;
using System.Dynamic;

namespace Vibe;
/// <summary>
/// Wraps an object to make it dynamically accesible
/// </summary>
public class Expansion : DynamicObject
{
    private readonly object _instance;
    /// <summary>
    /// Default Constuctor updates the instance when a value changes
    /// </summary>
    /// <param name="instance"></param>
    public Expansion(object instance)
    {
        _instance = instance;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        var property = _instance.GetType().GetProperty(binder.Name);
        if (property != null)
        {
            result = property.GetValue(_instance);
            return true;
        }

        result = null;
        return false;
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        var property = _instance.GetType().GetProperty(binder.Name);
        if (property != null)
        {
            property.SetValue(_instance, value);
            return true;
        }

        return false;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        var method = _instance.GetType().GetMethod(binder.Name);
        if (method != null)
        {
            result = method.Invoke(_instance, args);
            return true;
        }

        result = null;
        return false;
    }
}
