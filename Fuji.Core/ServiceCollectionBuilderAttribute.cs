﻿namespace Fuji;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceCollectionBuilderAttribute : Attribute
{
    public bool IncludeAllServices { get; set; }
    public Type? IncludeInterfaceImplementors { get; set; }
    public Type? IncludeClassInheritors { get; set; }
}