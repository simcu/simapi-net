using System;

namespace SimApi.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SynapseEventAttribute : Attribute
{
    public string Name { get; }

    public SynapseEventAttribute(string name)
    {
        Name = name;
    }
}