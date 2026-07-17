using System;
using System.Text.RegularExpressions;
using Obfuscar;
using Xunit;
using MethodAttributes = System.Reflection.MethodAttributes;
using FieldAttributes = System.Reflection.FieldAttributes;

namespace ObfuscarTests
{
    public class PredicateCollectionAndTesterTests
    {
        // ── PredicateCollection ──────────────────────────────────────────────

        [Fact]
        public void IsMatchReturnsFalseForEmptyCollection()
        {
            var col = new PredicateCollection<TypeKey>();
            var key = new TypeKey("scope", "ns", "name");
            Assert.False(col.IsMatch(key));
        }

        [Fact]
        public void IsMatchReturnsTrueWhenAnyPredicateMatches()
        {
            var col = new PredicateCollection<TypeKey>();
            col.Add(new TypeTester("ns.name"));
            col.Add(new TypeTester("other"));
            var key = new TypeKey("scope", "ns", "name");
            Assert.True(col.IsMatch(key));
        }

        [Fact]
        public void IsMatchReturnsFalseWhenNoPredicateMatches()
        {
            var col = new PredicateCollection<TypeKey>();
            col.Add(new TypeTester("ns.nope"));
            col.Add(new TypeTester("other"));
            var key = new TypeKey("scope", "ns", "name");
            Assert.False(col.IsMatch(key));
        }

        [Fact]
        public void IsMatchPassesInheritMapToPredicates()
        {
            var col = new PredicateCollection<TypeKey>();
            col.Add(new TypeTester("ns.name"));
            var key = new TypeKey("scope", "ns", "name");
            Assert.True(col.IsMatch(key, null));
        }

        // ── CheckMemberVisibility ────────────────────────────────────────────

        [Fact]
        public void CheckMemberVisibilityNoAttribReturnsFalse()
        {
            // No filtering specified → rule proceeds normally
            Assert.False(MethodTester.CheckMemberVisibility("", "",
                MethodAttributes.Public, null));

            Assert.False(MethodTester.CheckMemberVisibility(null, null,
                MethodAttributes.Private, null));
        }

        [Fact]
        public void CheckMemberVisibilityPublicAttribMatchesPublicMethod()
        {
            // attrib="public" + method is Public → matches, rule proceeds
            Assert.False(MethodTester.CheckMemberVisibility("public", "",
                MethodAttributes.Public, null));
        }

        [Fact]
        public void CheckMemberVisibilityPublicAttribRejectsNonPublic()
        {
            // attrib="public" + method is not Public → doesn't match
            Assert.True(MethodTester.CheckMemberVisibility("public", "",
                MethodAttributes.Private, null));

            Assert.True(MethodTester.CheckMemberVisibility("public", "",
                MethodAttributes.Assembly, null));

            Assert.True(MethodTester.CheckMemberVisibility("public", "",
                MethodAttributes.Family, null));
        }

        [Fact]
        public void CheckMemberVisibilityProtectedAttribMatchesProtectedFamily()
        {
            // attrib="protected" matches method that is Public, Family, or FamORAssem
            Assert.False(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.Public, null));

            Assert.False(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.Family, null));

            Assert.False(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.FamORAssem, null));
        }

        [Fact]
        public void CheckMemberVisibilityProtectedAttribRejectsPrivate()
        {
            // attrib="protected" does NOT match Private, Assembly, FamANDAssem
            Assert.True(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.Private, null));

            Assert.True(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.Assembly, null));

            Assert.True(MethodTester.CheckMemberVisibility("protected", "",
                MethodAttributes.FamANDAssem, null));
        }

        [Fact]
        public void CheckMemberVisibilityThrowsForInvalidAttrib()
        {
            var ex = Assert.Throws<ObfuscarException>(() =>
                MethodTester.CheckMemberVisibility("invalid", "",
                    MethodAttributes.Public, null));
            Assert.Contains("'invalid' is not valid", ex.Message);
        }

        [Fact]
        public void CheckMemberVisibilityThrowsForInvalidTypeAttrib()
        {
            var ex = Assert.Throws<ObfuscarException>(() =>
                MethodTester.CheckMemberVisibility("", "invalid",
                    MethodAttributes.Public, null));
            Assert.Contains("'invalid' is not valid", ex.Message);
        }

        [Fact]
        public void CheckMemberVisibilityTypeAttribPublicWithPublicType()
        {
            // When typeAttrib="public" and type is public, returns false (proceed)
            var publicDesc = new TypeDescriptor(null, "MyType", true, false, false, false, false, null);
            Assert.False(MethodTester.CheckMemberVisibility("", "public",
                MethodAttributes.Private, publicDesc));
        }

        [Fact]
        public void CheckMemberVisibilityTypeAttribPublicWithNonPublicType()
        {
            // When typeAttrib="public" and type is not public, returns false (no rejection)
            var nonPublicDesc = new TypeDescriptor(null, "MyType", false, false, false, false, false, null);
            Assert.False(MethodTester.CheckMemberVisibility("", "public",
                MethodAttributes.Private, nonPublicDesc));
        }

        [Fact]
        public void CheckMemberVisibilityBothAttribAndTypeAttrib()
        {
            var publicDesc = new TypeDescriptor(null, "MyType", true, false, false, false, false, null);
            // attrib="public" + typeAttrib="public" + public method + public type → proceed
            Assert.False(MethodTester.CheckMemberVisibility("public", "public",
                MethodAttributes.Public, publicDesc));

            // attrib="public" + private method → rejected regardless of typeAttrib
            Assert.True(MethodTester.CheckMemberVisibility("public", "public",
                MethodAttributes.Private, publicDesc));
        }

        // ── TypeTester ───────────────────────────────────────────────────────

        [Fact]
        public void TypeTesterMatchesByName()
        {
            var tester = new TypeTester("ns.name");
            var key = new TypeKey("scope", "ns", "name");
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void TypeTesterRejectsByName()
        {
            var tester = new TypeTester("other");
            var key = new TypeKey("scope", "ns", "name");
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void TypeTesterMatchesByRegex()
        {
            var tester = new TypeTester(new Regex("^ns\\.n.*e$"), TypeAffectFlags.SkipNone, string.Empty);
            var key = new TypeKey("scope", "ns", "name");
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void TypeTesterRejectsByRegex()
        {
            var tester = new TypeTester(new Regex("^other$"), TypeAffectFlags.SkipNone, string.Empty);
            var key = new TypeKey("scope", "ns", "name");
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void TypeTesterThrowsForInvalidAttrib()
        {
            var tester = new TypeTester("ns.name", TypeAffectFlags.SkipNone, "invalid");
            var key = new TypeKey("scope", "ns", "name");
            Assert.Throws<ObfuscarException>(() => tester.Test(key, null));
        }

        [Fact]
        public void TypeTesterRejectsNonPublicForPublicAttrib()
        {
            // FromFullName creates descriptor with IsPublic=false
            var tester = new TypeTester("ns.name", TypeAffectFlags.SkipNone, "public");
            var key = new TypeKey("scope", "ns", "name");
            Assert.False(tester.Test(key, null));
        }

        // ── MethodTester ─────────────────────────────────────────────────────

        [Fact]
        public void MethodTesterMatchesByName()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new MethodKey(typeKey,
                new MethodDefinition("TestMethod", MethodAttributes.Public, mock));

            var tester = new MethodTester("TestMethod", "ns.name", "", "");
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void MethodTesterRejectsByName()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new MethodKey(typeKey,
                new MethodDefinition("TestMethod", MethodAttributes.Public, mock));

            var tester = new MethodTester("Other", "ns.name", "", "");
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void MethodTesterMatchesByRegex()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new MethodKey(typeKey,
                new MethodDefinition("TestMethod", MethodAttributes.Public, mock));

            var tester = new MethodTester(new Regex("Test.*"), "ns.name", "", "");
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void MethodTesterRejectsByType()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new MethodKey(typeKey,
                new MethodDefinition("TestMethod", MethodAttributes.Public, mock));

            var tester = new MethodTester("TestMethod", "other", "", "");
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void MethodTesterMatchesByExactKey()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, mock);
            var key = new MethodKey(typeKey, method);

            var tester = new MethodTester(key);
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void MethodTesterRejectsByExactKey()
        {
            var typeKey1 = new TypeKey("scope", "ns", "name");
            var typeKey2 = new TypeKey("scope", "ns", "other");
            var mock = new TypeReference(string.Empty, "type", null);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, mock);
            var key1 = new MethodKey(typeKey1, method);
            var key2 = new MethodKey(typeKey2, method);

            var tester = new MethodTester(key1);
            // key2 has different TypeKey, so should not match
            Assert.False(tester.Test(key2, null));
        }

        // ── FieldTester ──────────────────────────────────────────────────────

        [Fact]
        public void FieldTesterMatchesByName()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new FieldKey(typeKey, "type", "TestField",
                new FieldDefinition("TestField", FieldAttributes.Public, mock)
                {
                    DeclaringType = new TypeDefinition(string.Empty, "type", TypeAttributes.Public, null)
                });

            var tester = new FieldTester("TestField", "ns.name", "", "", "", "", null, null);
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void FieldTesterRejectsByName()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new FieldKey(typeKey, "type", "TestField",
                new FieldDefinition("TestField", FieldAttributes.Public, mock)
                {
                    DeclaringType = new TypeDefinition(string.Empty, "type", TypeAttributes.Public, null)
                });

            var tester = new FieldTester("Other", "ns.name", "", "", "", "", null, null);
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void FieldTesterRejectsByType()
        {
            var typeKey = new TypeKey("scope", "ns", "name");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new FieldKey(typeKey, "type", "TestField",
                new FieldDefinition("TestField", FieldAttributes.Public, mock)
                {
                    DeclaringType = new TypeDefinition(string.Empty, "type", TypeAttributes.Public, null)
                });

            var tester = new FieldTester("TestField", "other", "", "", "", "", null, null);
            Assert.False(tester.Test(key, null));
        }

        [Fact]
        public void FieldTesterMatchesStaticFields()
        {
            var typeKey = new TypeKey("scope", "ns", "StaticType");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new FieldKey(typeKey, "type", "StaticField",
                new FieldDefinition("StaticField", FieldAttributes.Public | FieldAttributes.Static, mock)
                {
                    DeclaringType = new TypeDefinition(string.Empty, "type", TypeAttributes.Public, null)
                });

            var tester = new FieldTester("StaticField", "ns.StaticType", "", "", "", "", true, null);
            Assert.True(tester.Test(key, null));
        }

        [Fact]
        public void FieldTesterRejectsNonStaticWhenStaticExpected()
        {
            var typeKey = new TypeKey("scope", "ns", "InstanceType");
            var mock = new TypeReference(string.Empty, "type", null);
            var key = new FieldKey(typeKey, "type", "InstanceField",
                new FieldDefinition("InstanceField", FieldAttributes.Public, mock)
                {
                    DeclaringType = new TypeDefinition(string.Empty, "type", TypeAttributes.Public, null)
                });

            var tester = new FieldTester("InstanceField", "ns.InstanceType", "", "", "", "", true, null);
            Assert.False(tester.Test(key, null));
        }
    }
}
