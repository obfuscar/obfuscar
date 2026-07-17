using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EnumerableParameterIsSetToNullTest
{
    [Obfuscation]
    public interface IEnumerableParameterIsSetToNullExample_Interface
    {
        [Obfuscation]
        Task<bool> PublicMethodAsync();
    }

    public abstract class EnumerableParameterIsSetToNullExample_AbstractClass : IEnumerableParameterIsSetToNullExample_Interface
    {
        [Obfuscation]
        public async Task<bool> PublicMethodAsync()
        {
            var list = new List<EnumerableParameterIsSetToNullExample_ItemClass>();
            list.Add(new EnumerableParameterIsSetToNullExample_ItemClass());

            return
                await _ListAsync(list)
                &&
                await _EnumerableAsync(list)
                &&
                await _CollectionAsync(list)
                &&
                await _ArrayAsync(list.ToArray());
        }

        [Obfuscation]
        private async Task<bool> _ListAsync(List<EnumerableParameterIsSetToNullExample_ItemClass> list)
        {
            if (list.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        private async Task<bool> _EnumerableAsync(IEnumerable<EnumerableParameterIsSetToNullExample_ItemClass> enumerable)
        {
            if (enumerable.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        private async Task<bool> _CollectionAsync(ICollection<EnumerableParameterIsSetToNullExample_ItemClass> collection)
        {
            if (collection.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        private async Task<bool> _ArrayAsync(EnumerableParameterIsSetToNullExample_ItemClass[] array)
        {
            if (array.Any())
            {
                return true;
            }

            return false;
        }
    }

    [Obfuscation]
    public class EnumerableParameterIsSetToNullExample_ChildClass : EnumerableParameterIsSetToNullExample_AbstractClass
    {
    }

    [Obfuscation]
    public class EnumerableParameterIsSetToNullExample_ItemClass
    {
        public string SomeProperty { get; set; }
    }
}
