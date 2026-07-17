using System;
using System.Reflection;

namespace TestClasses
{
    // Type with Obfuscation attribute (to verify it's cleaned in output)
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class SkippedByAttribute
    {
        public int FieldA;
        public void MethodA() { }
    }

    // Type marked to rename (to verify attribute is still cleaned even when renamed)
    [Obfuscation(Exclude = false)]
    public class RenamedByAttribute
    {
        public void MethodB() { }
    }

    // Type with one method excluded (for ForceMethod test)
    public class ForceMethodTarget
    {
        public void NormalMethod() { }

        [Obfuscation(Exclude = true)]
        public void ExcludedByAttribute() { }
    }

    // Type where Private Methods are skipped by HidePrivateApi
    public class ForceMethodPrivateTarget
    {
        private void PrivateMethod() { }

        internal void InternalMethod() { }
    }

    // Type with properties and events (for SkipSpecialName test)
    public class SpecialNameTarget
    {
        public int PropA { get; set; }

        public int PropB
        {
            get { return _propB; }
            set { _propB = value; }
        }
        private int _propB;

        public event EventHandler MyEvent
        {
            add { }
            remove { }
        }
    }

    // Type with ForceField, ForceProperty, ForceEvent targets
    public class ForceOtherTarget
    {
        private int _fieldToForce;

        public int PropertyToForce { get; set; }

        public event EventHandler EventToForce;
    }

    public static class RulesTestEntryPoint
    {
        public static string Test()
        {
            var a = new SkippedByAttribute();
            a.MethodA();
            var b = new RenamedByAttribute();
            b.MethodB();
            return "ok";
        }
    }
}