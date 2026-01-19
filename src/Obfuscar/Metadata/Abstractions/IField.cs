namespace Obfuscar.Metadata.Abstractions
{
    public interface IField
    {
        string Name { get; }
        string FieldTypeFullName { get; }
        string DeclaringTypeFullName { get; }
        bool IsStatic { get; }
        System.Reflection.FieldAttributes Attributes { get; }
        bool HasCustomAttributes { get; }
        System.Collections.Generic.IEnumerable<string> CustomAttributeTypeFullNames { get; }
    }
}
