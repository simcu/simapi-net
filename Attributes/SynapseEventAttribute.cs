using System;

namespace SimApi.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SynapseEventAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}