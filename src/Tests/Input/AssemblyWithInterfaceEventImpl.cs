using System;
using TestLib;

namespace TestClasses
{
    internal class EventImplementation : ITest
    {
        private static int s_count;

        public event Action TestEvent;

        private static void OnEvent()
        {
            s_count++;
        }

        public int Run()
        {
            s_count = 0;
            TestEvent += new Action(OnEvent);
            TestEvent?.Invoke();
            return s_count;
        }
    }

    public static class EventEntryPoint
    {
        public static int ExecuteThroughInterface()
        {
            ITest test = new EventImplementation();
            test.TestEvent += new Action(StaticHandler);
            return ((EventImplementation)test).Run();
        }

        private static void StaticHandler()
        {
        }
    }
}
