namespace ObfuscarTestNet.Input
{
    public static class ContainsOverloads
    {
        public static void Method<T>(T[] numbers) { }
        public static void Method(double[] numbers) { }
    }
}
