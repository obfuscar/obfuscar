using System;
using System.Reflection;

namespace TestClasses
{
    [Obfuscation(ApplyToMembers = true)]
    public class AnonymousWithObfuscationOnClass
    {
        public AnonymousWithObfuscationOnClass()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }

        public void MethodWithLambda()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class AnonymousWithoutAttribute
    {
        public AnonymousWithoutAttribute()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }

        public void MethodWithLambda()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class AnonymousWithMethodLevelAttribute
    {
        public AnonymousWithMethodLevelAttribute()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }

        [Obfuscation]
        public void MethodWithObfuscationAttribute()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    // Lambda with captured variable -> creates <>c__DisplayClass
    public class AnonymousWithClosure
    {
        public int MethodWithCapturedVariable(int multiplier)
        {
            Func<int, int> doubleFunc = x => x * multiplier;
            return doubleFunc(10);
        }

        public int MethodWithMultipleCaptures(int a, int b)
        {
            Func<int, int> addFunc = x => x + a + b;
            return addFunc(10);
        }

        public int MethodWithMultipleLambdas(int offset)
        {
            Func<int, int> add = x => x + offset;
            Func<int, int> sub = x => x - offset;
            return add(10) + sub(20);
        }
    }

    // Multiple lambda patterns in one class
    public class AnonymousWithLinq
    {
        public int MethodWithMultipleLambdas(int offset)
        {
            System.Func<int, int> add = x => x + offset;
            System.Func<int, int> sub = x => x - offset;
            return add(10) + sub(20);
        }
    }

    // Static lambda (C# 9+)
    public class AnonymousWithStaticLambda
    {
        public int MethodWithStaticLambda()
        {
            Func<int, int, int> add = static (a, b) => a + b;
            return add(3, 4);
        }
    }

    // Multiple classes to generate multiple <>c types
    public class ReprocClass1
    {
        public ReprocClass1()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class ReprocClass2
    {
        public ReprocClass2()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class ReprocClass3
    {
        public ReprocClass3()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class ReprocClass4
    {
        public ReprocClass4()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public class ReprocClass5
    {
        public ReprocClass5()
        {
            var action1 = new Action<object>(
                (object _) => { var a = 1 + 2; }
            );
        }
    }

    public static class AnonymousMethodEntryPoint
    {
        public static string Test()
        {
            var obj1 = new AnonymousWithObfuscationOnClass();
            obj1.MethodWithLambda();
            var obj2 = new AnonymousWithoutAttribute();
            obj2.MethodWithLambda();
            var obj3 = new AnonymousWithMethodLevelAttribute();
            obj3.MethodWithObfuscationAttribute();
            var obj4 = new AnonymousWithClosure();
            obj4.MethodWithCapturedVariable(5);
            obj4.MethodWithMultipleCaptures(1, 2);
            obj4.MethodWithMultipleLambdas(3);
            var obj5 = new AnonymousWithLinq();
            obj5.MethodWithMultipleLambdas(3);
            var obj6 = new AnonymousWithStaticLambda();
            obj6.MethodWithStaticLambda();
            var obj7 = new ReprocClass1();
            var obj8 = new ReprocClass2();
            var obj9 = new ReprocClass3();
            var obj10 = new ReprocClass4();
            var obj11 = new ReprocClass5();
            return "ok";
        }
    }
}
