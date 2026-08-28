namespace ObfuscarTestNet.Input
{
    public static class CallToExternalOverloadedMethods
    {
        public static void ArrayCall() => ContainsOverloads.Method([1.0]);
        public static void GenericArrayCall() => ContainsOverloads.Method<double>([2.0]);
    }
}
