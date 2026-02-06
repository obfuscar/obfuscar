using System.Reflection;

namespace TestClasses
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class AnonymousTypePropertyEntryPoint
    {
        public static int Execute()
        {
            object args = new { offset = 3, sort_dir = "6" };
            return args.GetType().GetProperties().Length;
        }
    }
}
