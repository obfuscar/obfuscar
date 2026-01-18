using System;

namespace Obfuscar.Metadata.Abstractions
{
    [Flags]
    public enum MethodSemantics
    {
        None = 0x0,
        Setter = 0x1,
        Getter = 0x2,
        Other = 0x4,
        AddOn = 0x8,
        RemoveOn = 0x10,
        Fire = 0x20
    }
}
