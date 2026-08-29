// This example covers two bugs:
// 1.  IEnumerable|List|ICollection parameter is not used in method.
//     Instead new 'source' variable declared and is set as NULL:
//          List<ParameterIsSetToNullExample_ItemClass> source = default(List<ParameterIsSetToNullExample_ItemClass>);
// 2. System.MissingFieldException: Field not found: 'C.list'.
//      But 'list' is parameter, not a field.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ParameterIsSetToNullTestNamespace
{
    [Obfuscation]
    public interface IParameterIsSetToNullExample_Interface
    {
        Task<bool> PublicMethod_ListAsync();

        Task<bool> PublicMethod_EnumerableAsync();

        Task<bool> PublicMethod_CollectionAsync();

        Task<bool> PublicMethod_ArrayAsync();

        Task<bool> PublicMethod_NullableBooleanAsync();

        Task<bool> PublicMethod_NullableIntAsync();

        Task<bool> PublicMethod_NullableDateTimeAsync();

        Task<bool> PublicMethod_NullableTimeSpanAsync();
    }

    // [Obfuscation]
    public abstract class ParameterIsSetToNullExample_AbstractClass : IParameterIsSetToNullExample_Interface
    {
        [Obfuscation]
        public async Task<bool> PublicMethod_ListAsync()
        {
            var list = new List<ParameterIsSetToNullExample_ItemClass>();
            list.Add(new ParameterIsSetToNullExample_ItemClass());

            return await _ListAsync(list);
        }

        [Obfuscation]
        private async Task<bool> _ListAsync(List<ParameterIsSetToNullExample_ItemClass> list)
        {
            if (list.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_EnumerableAsync()
        {
            var list = new List<ParameterIsSetToNullExample_ItemClass>();
            list.Add(new ParameterIsSetToNullExample_ItemClass());

            return await _EnumerableAsync(list);
        }

        [Obfuscation]
        private async Task<bool> _EnumerableAsync(IEnumerable<ParameterIsSetToNullExample_ItemClass> enumerable)
        {
            if (enumerable.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_CollectionAsync()
        {
            var list = new List<ParameterIsSetToNullExample_ItemClass>();
            list.Add(new ParameterIsSetToNullExample_ItemClass());

            return await _CollectionAsync(list);
        }

        [Obfuscation]
        private async Task<bool> _CollectionAsync(ICollection<ParameterIsSetToNullExample_ItemClass> collection)
        {
            if (collection.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_ArrayAsync()
        {
            var list = new List<ParameterIsSetToNullExample_ItemClass>();
            list.Add(new ParameterIsSetToNullExample_ItemClass());

            return await _ArrayAsync(list.ToArray());
        }

        [Obfuscation]
        private async Task<bool> _ArrayAsync(ParameterIsSetToNullExample_ItemClass[] array)
        {
            if (array.Any())
            {
                return true;
            }

            return false;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_NullableBooleanAsync()
        {
            return await _NullableBooleanAsync(true);
        }

        [Obfuscation]
        private async Task<bool> _NullableBooleanAsync(bool? boolean)
        {
            return boolean ?? false;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_NullableIntAsync()
        {
            return await _NullableIntAsync(1);
        }

        [Obfuscation]
        private async Task<bool> _NullableIntAsync(int? value)
        {
            return value > 0;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_NullableDateTimeAsync()
        {
            return await _NullableDateTimeAsync(DateTime.Now);
        }

        [Obfuscation]
        private async Task<bool> _NullableDateTimeAsync(DateTime? value)
        {
            return value > DateTime.MinValue;
        }

        [Obfuscation]
        public async Task<bool> PublicMethod_NullableTimeSpanAsync()
        {
            return await _NullableTimeSpanAsync(TimeSpan.FromSeconds(1));
        }

        [Obfuscation]
        private async Task<bool> _NullableTimeSpanAsync(TimeSpan? value)
        {
            return value?.TotalSeconds > 0;
        }
    }

    [Obfuscation]
    public class ParameterIsSetToNullExample_ChildClass : ParameterIsSetToNullExample_AbstractClass
    {
    }

    [Obfuscation]
    public class ParameterIsSetToNullExample_ItemClass
    {
        public string SomeProperty { get; set; }
    }
}
