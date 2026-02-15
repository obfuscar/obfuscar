using System.Runtime.InteropServices;

namespace TestClasses
{
    public class NativeMethods
    {
        [DllImport("test")]
        private static extern int Roll(int a, bool b);

        public static int CallRoll(int a, bool b)
        {
            return Roll(a, b);
        }
    }
}
