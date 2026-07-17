using System.IO;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    [Collection("NameMaker")]
    public class NameMakerTests
    {
        const string CustomCharSet = "ABC";

        private static Settings MakeSettingsWithCustomChars()
        {
            var vars = new Variables();
            vars.Add("InPath", Path.GetTempPath());
            vars.Add("OutPath", Path.GetTempPath());
            vars.Add("CustomChars", CustomCharSet);
            return new Settings(vars);
        }

        public NameMakerTests()
        {
            NameMaker.DetermineChars(MakeSettingsWithCustomChars());
        }

        [Fact]
        public void UniqueNameReturnsSingleCharForSmallIndex()
        {
            Assert.Equal("A", NameMaker.UniqueName(0));
            Assert.Equal("B", NameMaker.UniqueName(1));
            Assert.Equal("C", NameMaker.UniqueName(2));
        }

        [Fact]
        public void UniqueNameWrapsToMultiCharWhenIndexExceedsBase()
        {
            // CustomCharSet = "ABC" (3 chars), index 3 = first multi-char name
            // 3 % 3 = 0 => 'A', 3 / 3 = 1 => stack push 'A', then 'B'
            // Pop order: 'B', 'A' => "BA"
            Assert.Equal("BA", NameMaker.UniqueName(3));

            // index 4: 4 % 3 = 1 => 'B', 4 / 3 = 1 => push 'B', then 'B' => "BB"
            Assert.Equal("BB", NameMaker.UniqueName(4));

            // index 5: 5 % 3 = 2 => 'C', 5 / 3 = 1 => push 'C', then 'B' => "BC"
            Assert.Equal("BC", NameMaker.UniqueName(5));
        }

        [Fact]
        public void UniqueNameHandlesLargerIndexWithMoreDigits()
        {
            // chars = "ABC" (3), numUniqueChars = 3
            // 9 = 1*3^2 + 0*3 + 0 => 1,0,0 => 'B','A','A'
            // stack: push 'A' (9%3=0), 9/3=3; push 'A' (3%3=0), 3/3=1; push 'B' (1%3=1), 1<3 break
            // Pop: 'B', 'A', 'A' => "BAA"
            Assert.Equal("BAA", NameMaker.UniqueName(9));
        }

        [Fact]
        public void UniqueNameWithSeparatorInsertsSeparator()
        {
            // index 3 => base-3 = "BA", with sep "." => "B.A"
            Assert.Equal("B.A", NameMaker.UniqueName(3, "."));

            // index 0 => single char, no separator needed
            Assert.Equal("A", NameMaker.UniqueName(0, "."));
        }

        [Fact]
        public void UniqueTypeNameUsesModulo()
        {
            // With 3 chars: UniqueTypeName(0) = UniqueName(0%3, ".") = "A"
            // UniqueTypeName(3) = UniqueName(3%3, ".") = UniqueName(0, ".") = "A"
            // UniqueTypeName(4) = UniqueName(4%3, ".") = UniqueName(1, ".") = "B"
            Assert.Equal("A", NameMaker.UniqueTypeName(0));
            Assert.Equal("A", NameMaker.UniqueTypeName(3));
            Assert.Equal("B", NameMaker.UniqueTypeName(4));
        }

        [Fact]
        public void UniqueNamespaceUsesDivision()
        {
            // With 3 chars: UniqueNamespace(0) = UniqueName(0/3, ".") = UniqueName(0, ".") = "A"
            // UniqueNamespace(1) = UniqueName(1/3, ".") = UniqueName(0, ".") = "A"
            // UniqueNamespace(2) = UniqueName(2/3, ".") = UniqueName(0, ".") = "A"
            // UniqueNamespace(3) = UniqueName(3/3, ".") = UniqueName(1, ".") = "B"
            Assert.Equal("A", NameMaker.UniqueNamespace(0));
            Assert.Equal("A", NameMaker.UniqueNamespace(1));
            Assert.Equal("A", NameMaker.UniqueNamespace(2));
            Assert.Equal("B", NameMaker.UniqueNamespace(3));
        }

        [Fact]
        public void UniqueNestedTypeNameSameAsUniqueName()
        {
            Assert.Equal(NameMaker.UniqueName(0), NameMaker.UniqueNestedTypeName(0));
            Assert.Equal(NameMaker.UniqueName(3), NameMaker.UniqueNestedTypeName(3));
        }

        [Fact]
        public void DetermineCharsUsesCustomChars()
        {
            Assert.Equal(CustomCharSet, NameMaker.UniqueChars);
        }
    }

    [Collection("NameMaker")]
    public class NameGroupTests
    {
        const string DefaultChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        private static Settings MakeDefaultSettings()
        {
            var vars = new Variables();
            vars.Add("InPath", Path.GetTempPath());
            vars.Add("OutPath", Path.GetTempPath());
            return new Settings(vars);
        }

        public NameGroupTests()
        {
            NameMaker.DetermineChars(MakeDefaultSettings());
        }

        [Fact]
        public void GetNextReturnsNameNotInGroup()
        {
            var group = new NameGroup();
            group.Add("A");
            // After adding "A", GetNext should return the next available name
            Assert.Equal("a", group.GetNext());
        }

        [Fact]
        public void GetNextSkipsExistingNames()
        {
            var group = new NameGroup();
            group.Add("A");
            group.Add("a");

            // First call should skip "A" (index 0) and "a" (index 1), returning "B" (index 2)
            Assert.Equal("B", group.GetNext());
        }

        [Fact]
        public void ContainsChecksMembership()
        {
            var group = new NameGroup();
            group.Add("test");
            Assert.True(group.Contains("test"));
            Assert.False(group.Contains("other"));
        }

        [Fact]
        public void AddAllAddsMultipleNames()
        {
            var group = new NameGroup();
            group.AddAll(new[] { "x", "y", "z" });
            Assert.True(group.Contains("x"));
            Assert.True(group.Contains("y"));
            Assert.True(group.Contains("z"));
        }

        [Fact]
        public void GetNextWithGroupsSkipsNamesFromAllGroups()
        {
            var group1 = new NameGroup();
            group1.Add("A");
            group1.Add("a");

            var group2 = new NameGroup();
            group2.Add("B");

            // GetNext across both groups should skip "A" (0), "a" (1), "B" (2) and return "b" (3)
            var result = NameGroup.GetNext(new[] { group1, group2 });
            Assert.Equal("b", result);
        }

        [Fact]
        public void GetNextHandlesEmptyGroup()
        {
            var group = new NameGroup();
            // Should return first name since group is empty
            Assert.Equal("A", group.GetNext());
        }

        [Fact]
        public void RemoveRemovesName()
        {
            var group = new NameGroup();
            group.Add("test");
            Assert.True(group.Contains("test"));
            group.Remove("test");
            Assert.False(group.Contains("test"));
        }
    }
}
