using System;
using System.Reflection;

namespace TestClasses
{
    public sealed class AudioManager
    {
        private static readonly AudioManager Value = new AudioManager();

        public static AudioManager Instance => Value;

        public int SomeMethod(int i)
        {
            return i + 7;
        }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class LambdaCapturedReferenceEntryPoint
    {
        public static int Execute()
        {
            int captured = 5;
            Func<int, int> mapper = x =>
            {
                int value = captured + x;
                return AudioManager.Instance.SomeMethod(value);
            };

            return mapper(3);
        }
    }
}
