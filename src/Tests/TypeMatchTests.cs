using LeXtudio.Metadata.Mutable;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class TypeMatchTests
    {
        static readonly TypeReference StringType = new TypeReference("System", "String", null);
        static readonly TypeReference Int32Type = new TypeReference("System", "Int32", null);
        static readonly TypeReference ObjectType = new TypeReference("System", "Object", null);

        // ── MethodMatch: basic cases ─────────────────────────────────────────

        [Fact]
        public void MethodMatchReturnsTrueForIdenticalMethods()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m1.Parameters.Add(new ParameterDefinition("p", ParameterAttributes.None, Int32Type));

            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m2.Parameters.Add(new ParameterDefinition("p", ParameterAttributes.None, Int32Type));

            Assert.True(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchRejectsDifferentName()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            var m2 = new MethodDefinition("Bar", MethodAttributes.Public, StringType);

            Assert.False(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchRejectsDifferentReturnType()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, Int32Type);

            Assert.False(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchRejectsDifferentParameterCount()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m1.Parameters.Add(new ParameterDefinition("p", ParameterAttributes.None, Int32Type));

            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m2.Parameters.Add(new ParameterDefinition("p1", ParameterAttributes.None, Int32Type));
            m2.Parameters.Add(new ParameterDefinition("p2", ParameterAttributes.None, StringType));

            Assert.False(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchRejectsDifferentParameterType()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m1.Parameters.Add(new ParameterDefinition("p", ParameterAttributes.None, Int32Type));

            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m2.Parameters.Add(new ParameterDefinition("p", ParameterAttributes.None, StringType));

            Assert.False(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchAcceptsDifferentParameterName()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m1.Parameters.Add(new ParameterDefinition("p1", ParameterAttributes.None, Int32Type));

            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m2.Parameters.Add(new ParameterDefinition("p2", ParameterAttributes.None, Int32Type));

            // Names don't matter for override matching, only types
            Assert.True(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchHandlesNoParameters()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);

            Assert.True(MethodKey.MethodMatch(m1, m2));
        }

        [Fact]
        public void MethodMatchRejectsDifferentGenericParamCount()
        {
            var m1 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            m1.GenericParameters.Add(new MutableGenericParameter("T", null));

            var m2 = new MethodDefinition("Foo", MethodAttributes.Public, StringType);

            Assert.False(MethodKey.MethodMatch(m1, m2));
        }

        // ── TypeMatch: GenericParameter ──────────────────────────────────────

        [Fact]
        public void TypeMatchGenericParameterMatchesAnything()
        {
            // Any type against GenericParameter should match
            var gp = new MutableGenericParameter("T", null);
            Assert.True(CallTypeMatch(gp, StringType));
            Assert.True(CallTypeMatch(StringType, gp));
            Assert.True(CallTypeMatch(gp, gp));
        }

        // ── TypeMatch: ArrayType ──────────────────────────────────────────────

        [Fact]
        public void TypeMatchArrayWithSameRankAndElement()
        {
            var a1 = new MutableArrayType(StringType, 1);
            var a2 = new MutableArrayType(StringType, 1);
            Assert.True(CallTypeMatch(a1, a2));
        }

        [Fact]
        public void TypeMatchArrayRejectsDifferentRank()
        {
            var a1 = new MutableArrayType(StringType, 1);
            var a2 = new MutableArrayType(StringType, 2);
            Assert.False(CallTypeMatch(a1, a2));
        }

        [Fact]
        public void TypeMatchArrayRejectsDifferentElement()
        {
            var a1 = new MutableArrayType(StringType, 1);
            var a2 = new MutableArrayType(Int32Type, 1);
            Assert.False(CallTypeMatch(a1, a2));
        }

        [Fact]
        public void TypeMatchArrayVsNonArrayFails()
        {
            var array = new MutableArrayType(StringType, 1);
            Assert.False(CallTypeMatch(array, StringType));
            Assert.False(CallTypeMatch(StringType, array));
        }

        // ── TypeMatch: ByReferenceType ────────────────────────────────────────

        [Fact]
        public void TypeMatchByRefWithSameElement()
        {
            var br1 = new MutableByReferenceType(StringType);
            var br2 = new MutableByReferenceType(StringType);
            Assert.True(CallTypeMatch(br1, br2));
        }

        [Fact]
        public void TypeMatchByRefRejectsDifferentElement()
        {
            var br1 = new MutableByReferenceType(StringType);
            var br2 = new MutableByReferenceType(Int32Type);
            Assert.False(CallTypeMatch(br1, br2));
        }

        [Fact]
        public void TypeMatchByRefVsNonByRefFails()
        {
            var br = new MutableByReferenceType(StringType);
            Assert.False(CallTypeMatch(br, StringType));
            Assert.False(CallTypeMatch(StringType, br));
        }

        // ── TypeMatch: PointerType ────────────────────────────────────────────

        [Fact]
        public void TypeMatchPointerWithSameElement()
        {
            var p1 = new MutablePointerType(StringType);
            var p2 = new MutablePointerType(StringType);
            Assert.True(CallTypeMatch(p1, p2));
        }

        [Fact]
        public void TypeMatchPointerRejectsDifferentElement()
        {
            var p1 = new MutablePointerType(StringType);
            var p2 = new MutablePointerType(Int32Type);
            Assert.False(CallTypeMatch(p1, p2));
        }

        [Fact]
        public void TypeMatchPointerVsNonPointerFails()
        {
            var ptr = new MutablePointerType(StringType);
            Assert.False(CallTypeMatch(ptr, StringType));
            Assert.False(CallTypeMatch(StringType, ptr));
        }

        // ── TypeMatch: Null / Same reference ──────────────────────────────────

        [Fact]
        public void TypeMatchReturnsTrueForBothNull()
        {
            Assert.True(CallTypeMatch(null, null));
        }

        [Fact]
        public void TypeMatchReturnsFalseForOneNull()
        {
            Assert.False(CallTypeMatch(null, StringType));
            Assert.False(CallTypeMatch(StringType, null));
        }

        // ── TypeMatch: Mixed types (cross-kind) ───────────────────────────────

        [Fact]
        public void TypeMatchMixedKindsAllFail()
        {
            var array = new MutableArrayType(StringType, 1);
            var byRef = new MutableByReferenceType(StringType);
            var ptr = new MutablePointerType(StringType);

            Assert.False(CallTypeMatch(array, byRef));
            Assert.False(CallTypeMatch(array, ptr));
            Assert.False(CallTypeMatch(byRef, ptr));
            Assert.False(CallTypeMatch(byRef, array));
            Assert.False(CallTypeMatch(ptr, array));
            Assert.False(CallTypeMatch(ptr, byRef));
        }

        // ── TypeMatch: FullName matching (simple types) ───────────────────────

        [Fact]
        public void TypeMatchSimpleTypesByFullName()
        {
            var s1 = new TypeReference("System", "String", null);
            var s2 = new TypeReference("System", "String", null);
            Assert.True(CallTypeMatch(s1, s2));
        }

        [Fact]
        public void TypeMatchRejectsDifferentSimpleTypes()
        {
            var s1 = new TypeReference("System", "String", null);
            var i1 = new TypeReference("System", "Int32", null);
            Assert.False(CallTypeMatch(s1, i1));
        }

        // ── GetGenericParameterCount ──────────────────────────────────────────

        [Fact]
        public void GetGenericParameterCountForSimpleMethod()
        {
            var method = new MethodDefinition("Foo", MethodAttributes.Public, StringType);
            method.GenericParameters.Add(new MutableGenericParameter("T", null));
            method.GenericParameters.Add(new MutableGenericParameter("U", null));

            // Non-generic-instance method counts its own GenericParameters
            var gm = new MutableGenericInstanceMethod(method);
            Assert.Equal(2, CallGetGenericParameterCount(gm));
        }

        // ── Helper: call private static MethodKey methods via reflection ───────

        private static bool CallTypeMatch(MutableTypeReference a, MutableTypeReference b)
        {
            var method = typeof(MethodKey).GetMethod("TypeMatch",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                new[] { typeof(MutableTypeReference), typeof(MutableTypeReference) });
            return (bool)method.Invoke(null, new object[] { a, b });
        }

        private static int CallGetGenericParameterCount(MutableMethodReference method)
        {
            var m = typeof(MethodKey).GetMethod("GetGenericParameterCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (int)m.Invoke(null, new object[] { method });
        }
    }
}
