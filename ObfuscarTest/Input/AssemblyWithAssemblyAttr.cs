[assembly: System.Reflection.ObfuscationAttribute(Exclude = false, Feature = "all")]

namespace TestClasses
{
    public enum PublicEnumA { A, B };

    public enum PublicEnumB { A, B };

	public class PublicClassA
	{
        public void PublicMethodA()
        {
        }

        public void PublicMethodB()
        {
        }

        public int PublicPropertyA { get; set; }

        public int PublicPropertyB { get; set; }
    }

    public class PublicClassB
    {
        public void PublicMethodA()
        {
        }

        public int PublicPropertyA { get; set; }
    }
}
