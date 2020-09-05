using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bicep.Types.Concrete;
using Bicep.Types.Serialized;

namespace Bicep.Types
{
    public static class TypeSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

        public static string Serialize(TypeBase[] types)
        {
            var flatConverter = new ToFlatTypeConverter(types);
            var flatTypes = types.Select(x => flatConverter.Convert(x)).ToArray();

            return JsonSerializer.Serialize(flatTypes, SerializerOptions);
        }

        public static TypeBase[] Deserialize(string content)
        {
            var flatTypes = JsonSerializer.Deserialize<FlatType[]>(content, SerializerOptions);
            var flatConverter = new FromFlatTypeConverter(flatTypes);

            return flatTypes.Select(x => flatConverter.Convert(x)).ToArray();
        }
    }
}