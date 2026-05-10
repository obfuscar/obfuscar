using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Reflection;

/**
 *   Call of Method1() of each class below will may cause the one of runtime exceptions: 
 *    1) Common Language Runtime detected an invalid program
 *    2) StackOverflowException    
 *    3) System.MethodAccessException
 */
namespace TestClasses
{

    /// <summary>
    /// Two public instance methods 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_1 
    {
        public string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }

        public string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// Two private instance methods 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_2 
    {
        private string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
        
        private string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// Two public static methods 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_3 
    {
        public static string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
        
        public static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// Two private static methods 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_4 
    {
        private static string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }

        private static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// One public instance method  and one public static method 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_5 
    {
        public string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
        
        public static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// One private instance method and one private static method 
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public class Class_6 
    {
        private string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
        
        private static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// Two public static methods in static class
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public static class Class_7 
    {
        public static string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
       
        public static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    /// <summary>
    /// Two private static methods in static class
    /// </summary>
    [Obfuscation(ApplyToMembers = false)]
    public static class Class_8 
    {
        private static string Method1(object param1, byte? param2)
        {
            return Method2(param1.ToString(), param2);
        }
        
        private static string Method1(string param1, byte? param2)
        {
            Counter.IncreaseMethod1CallCounter(ref param2);

            return Method2(param1, param2);
        }

        // same arguments as Method1
        private static string Method2(string param1, byte? param2)
        {
            return "private static string Method2(string param1, byte? param2)";
        }
    }

    public static class Counter
    {
        static int method1CallCounter = 0;

        /// <summary>
        /// Throws ApplicationException when possible StackOverflowException is detected.
        /// </summary>
        /// <remarks>
        /// In case of StackOverflowException the xUnit test will remain in 'not run' state. We need test to succeed or fail.
        /// </remarks>
        /// <param name="param2"></param>
        /// <exception cref="ApplicationException"></exception>
        public static void IncreaseMethod1CallCounter(ref byte? param2)
        {
            method1CallCounter++;

            if (param2 != null)
            {
                if (param2 == 0)
                {
                    throw new ApplicationException($"'param2'=0 is not allowed. Called {method1CallCounter} times. Possible StackOverflowException");
                }

                param2--;
            }
        }
    }


}
