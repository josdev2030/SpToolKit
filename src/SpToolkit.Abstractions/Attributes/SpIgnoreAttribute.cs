namespace SpToolkit.Abstractions.Attributes;

/// <summary>
/// Instructs the runtime to skip this property during parameter building and row materialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpIgnoreAttribute : Attribute { }
