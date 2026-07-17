using System;

namespace TestClasses
{
    // Direct overrides of System.Object methods
    public class ClassWithObjectOverrides
    {
        public override bool Equals(object obj)
        {
            return obj is ClassWithObjectOverrides other;
        }

        public override int GetHashCode()
        {
            return 42;
        }

        public override string ToString()
        {
            return nameof(ClassWithObjectOverrides);
        }
    }

    // Override Finalize (protected)
    public class ClassWithFinalizeOverride
    {
        ~ClassWithFinalizeOverride()
        {
            // Destructor generates Finalize override
        }
    }

    // Chain: External(System.Object) -> InternalBase -> InternalDerived
    // Both overrides should be skipped
    public class ExternalMethodOverrideBase
    {
        public override string ToString()
        {
            return "base";
        }
    }

    public class ExternalMethodOverrideDerived : ExternalMethodOverrideBase
    {
        public override string ToString()
        {
            return "derived";
        }
    }

    // Object.Equals overridden in a chain
    public class ChainedEqualsBase
    {
        public override bool Equals(object obj)
        {
            return obj is ChainedEqualsBase;
        }
    }

    public class ChainedEqualsDerived : ChainedEqualsBase
    {
        public override bool Equals(object obj)
        {
            return obj is ChainedEqualsDerived;
        }
    }

    public static class ObjectOverridesEntryPoint
    {
        public static string Test()
        {
            var a = new ClassWithObjectOverrides();
            var b = new ClassWithFinalizeOverride();
            var c = new ExternalMethodOverrideBase();
            var d = new ExternalMethodOverrideDerived();
            var e = new ChainedEqualsBase();
            var f = new ChainedEqualsDerived();
            a.Equals(b);
            c.Equals(d);
            e.Equals(f);
            return "ok";
        }
    }
}
