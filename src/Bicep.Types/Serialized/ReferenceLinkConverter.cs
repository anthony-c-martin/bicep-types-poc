using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bicep.Types.Concrete;

namespace Bicep.Types.Serialized
{
    public class TypeReferenceConverter : JsonConverter<ITypeReference>
    {
        private readonly ITypeReferenceResolver resolver;

        public TypeReferenceConverter(ITypeReferenceResolver resolver)
        {
            this.resolver = resolver;
        }

        public override ITypeReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }

            var index = reader.GetInt32();

            return resolver.GetTypeReference(index);
        }

        public override void Write(Utf8JsonWriter writer, ITypeReference value, JsonSerializerOptions options)
        {
            var index = resolver.GetIndex(value.Type);

            writer.WriteNumberValue(index);
        }
    }
}