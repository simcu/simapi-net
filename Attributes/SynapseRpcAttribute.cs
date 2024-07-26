using System;

namespace SimApi.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SynapseRpcAttribute : Attribute
{
    public string? Name { get; }

    public SynapseRpcAttribute()
    {
        Name = null;
    }

    public SynapseRpcAttribute(string name)
    {
        Name = name;
    }
}