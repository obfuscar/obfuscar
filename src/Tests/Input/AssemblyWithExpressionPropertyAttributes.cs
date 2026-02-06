using System;
using System.Linq.Expressions;
using System.Reflection;

namespace TestClasses
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class MarkerAttribute : Attribute
    {
        public MarkerAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public abstract class Base
    {
        [Marker("Base")]
        public abstract string Code { get; set; }
    }

    public sealed class Derived : Base
    {
        [Marker("Derived")]
        public override string Code { get; set; }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class ExpressionPropertyAttributeEntryPoint
    {
        public static string Execute()
        {
            string first = Resolve((Derived data) => data.Code);
            string second = Resolve((Base data) => data.Code);
            return first + "|" + second;
        }

        private static string Resolve<TObject, TValue>(Expression<Func<TObject, TValue>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;
            var property = memberExpression.Expression.Type.GetProperty(memberExpression.Member.Name);
            var attribute = property.GetCustomAttribute<MarkerAttribute>(false);
            return attribute.Value;
        }
    }
}
