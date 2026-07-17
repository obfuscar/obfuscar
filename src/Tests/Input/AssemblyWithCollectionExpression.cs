using System.Collections.Generic;

namespace TestClasses
{
    public class CollectionExpressionUser
    {
        private List<object> _items = new List<object>();

        public void LoadPreset(List<object> items)
        {
            _items = items;
        }

        public void CallWithCollectionExpression()
        {
            // C# 12 collection expression — compiler generates hidden
            // cached empty array field (<>p__0 or similar)
            LoadPreset([new object()]);
        }

        public void CallWithExplicitList()
        {
            LoadPreset(new List<object> { new object() });
        }

        public int Count => _items.Count;

        public static string Test()
        {
            var obj = new CollectionExpressionUser();
            obj.CallWithCollectionExpression();
            if (obj.Count != 1)
                return "fail: collection expression failed";
            obj.CallWithExplicitList();
            if (obj.Count != 1)
                return "fail: explicit list failed";
            return "ok";
        }
    }
}
