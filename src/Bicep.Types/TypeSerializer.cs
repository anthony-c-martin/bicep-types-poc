using System;
using System.Collections.Generic;
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
        private class TypeReferenceResolver : ITypeReferenceResolver
        {
            private class TypeReference : ITypeReference
            {
                private readonly TypeReferenceResolver resolver;
                private readonly int index;

                public TypeReference(TypeReferenceResolver resolver, int index)
                {
                    this.resolver = resolver;
                    this.index = index;
                }

                public TypeBase Type => resolver.types[index];
            }

            private TypeBase[] types;
            private IReadOnlyDictionary<TypeBase, int> reverseLookupDict;

            public TypeReferenceResolver(TypeBase[] types)
            {
                this.types = types;
                reverseLookupDict = Enumerable.Range(0, types.Length).ToDictionary(x => types[x], x => x);
            }

            public int GetIndex(TypeBase type)
                => reverseLookupDict[type];

            public ITypeReference GetTypeReference(int index)
                => new TypeReference(this, index);
        }

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

        public static string Serialize(TypeBase[] types)
        {
            var resolver = new TypeReferenceResolver(types);

            var serializeOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
            };
            serializeOptions.Converters.Add(new TypeBaseConverter(resolver));

            return JsonSerializer.Serialize(types, serializeOptions);
        }

        public static TypeBase[] Deserialize(string content)
        {
            var resolver = new TypeReferenceResolver(Array.Empty<TypeBase>());

            var serializeOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
            };
            serializeOptions.Converters.Add(new TypeBaseConverter(resolver));

            return JsonSerializer.Deserialize<TypeBase[]>(content, serializeOptions);
        }
    }
}