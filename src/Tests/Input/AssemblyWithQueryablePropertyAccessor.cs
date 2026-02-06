using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestClasses
{
    internal class QueryablePropertyAccessorModel
    {
        public string Name { get; set; }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class QueryablePropertyAccessorEntryPoint
    {
        public static int Execute()
        {
            var query = new List<QueryablePropertyAccessorModel> { new(), new(), new() }.AsQueryable();
            query = query.Where(a => a.Name == "john");
            return query.Count();
        }
    }
}
