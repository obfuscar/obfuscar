using System;
using System.Runtime.InteropServices;

namespace Issue599
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct MyStruct : IEquatable<MyStruct>
    {
        [FieldOffset(0)]
        public readonly uint Data;

        public MyStruct(uint data) => Data = data;

        public bool Equals(MyStruct other) => Data == other.Data;

        public override bool Equals(object? obj) => obj is MyStruct other && Equals(other);

        public override int GetHashCode() => Data.GetHashCode();

        public static uint GetDataValue() => new MyStruct(0xDEADBEEF).Data;
    }

    public static class EntryPoint
    {
        public static bool Execute() => MyStruct.GetDataValue() == 0xDEADBEEF;
    }
}
