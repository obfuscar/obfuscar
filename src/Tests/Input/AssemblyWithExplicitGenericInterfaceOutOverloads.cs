namespace Issue546
{
    public interface IFace<T>
    {
        string Method1(int a, T b);
        string Method2(T a, int b);
        string Method3(int a, out T b);
        string Method4(T a, out int b);
    }

    public class Implementation : IFace<int>
    {
        string IFace<int>.Method1(int a, int b) => "Method1";

        string IFace<int>.Method2(int a, int b) => "Method2";

        string IFace<int>.Method3(int a, out int b)
        {
            b = 42;
            return "Method3";
        }

        string IFace<int>.Method4(int a, out int b)
        {
            b = 42;
            return "Method4";
        }
    }

    public static class EntryPoint
    {
        public static int Execute()
        {
            var inst = (IFace<int>)new Implementation();
            var score = 0;
            if (inst.Method1(1, 2) == "Method1")
                score++;
            if (inst.Method2(1, 2) == "Method2")
                score++;
            if (inst.Method3(1, out var out1) == "Method3" && out1 == 42)
                score++;
            if (inst.Method4(1, out var out2) == "Method4" && out2 == 42)
                score++;

            return score;
        }
    }
}
