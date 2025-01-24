using System.Collections.Generic;
using System.Dynamic;

public class DynamicObjectHelper : DynamicObject
{
    private readonly IDictionary<string, object> _properties = new ExpandoObject();

    public void AddProperty(string name, object value)
    {
        _properties[name] = value;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return _properties.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        _properties[binder.Name] = value;
        return true;
    }

    public static dynamic CreateDynamicObject(IEnumerable<object> propertySources)
    {
        var result = new ExpandoObject() as IDictionary<string, object>;

        foreach (var source in propertySources)
        {
            if (source is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value is IEnumerable<object> nestedProperties
                        ? CreateDynamicObject(nestedProperties) // Recursive creation
                        : kvp.Value;
                }
            }
            else if (source is KeyValuePair<string, object> kvp)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public dynamic Spread(params dynamic[] objects)
    {
        var result = new ExpandoObject() as IDictionary<string, object>;

        foreach (var obj in objects)
        {
            if (obj is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }
}
