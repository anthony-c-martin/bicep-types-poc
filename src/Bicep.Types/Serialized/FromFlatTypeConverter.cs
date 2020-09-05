using System;
using System.Collections.Generic;
using System.Linq;
using Bicep.Types.Concrete;

namespace Bicep.Types.Serialized
{
    public class FromFlatTypeConverter
    {
        private class AllocatedTypeReference : ITypeReference
        {
            private readonly FromFlatTypeConverter converter;
            private readonly int index;

            public AllocatedTypeReference(FromFlatTypeConverter converter, int index)
            {
                this.converter = converter;
                this.index = index;
            }

            public TypeBase Type => converter.concreteTypes[index];
        }

        private ITypeReference GetReference(int index)
            => new AllocatedTypeReference(this, index);

        private IDictionary<int, TypeBase> concreteTypes;
        private IReadOnlyDictionary<int, FlatType> flatTypes;

        public FromFlatTypeConverter(IReadOnlyList<FlatType> flatTypes)
        {
            this.concreteTypes = new Dictionary<int, TypeBase>();
            this.flatTypes = flatTypes.ToDictionary(x => x.Index ?? throw new ArgumentException(nameof(flatTypes)));
        }

        public TypeBase Convert(FlatType flatType)
        {
            var index = flatType.Index ?? throw new ArgumentException(nameof(flatType));

            if (!concreteTypes.TryGetValue(index, out var value))
            {
                concreteTypes[index] = ConvertInternal(flatType);
                value = concreteTypes[index];
            }

            return value;            
        }

        private TypeBase ConvertInternal(FlatType flatType)
        {
            var kind = flatType.Kind ?? throw new ArgumentException(nameof(flatType));

            switch (kind)
            {
                case FlatTypeKind.Never:
                    return new BuiltInType(BuiltInTypeKind.Never);
                case FlatTypeKind.Any:
                    return new BuiltInType(BuiltInTypeKind.Any);
                case FlatTypeKind.Null:
                    return new BuiltInType(BuiltInTypeKind.Null);
                case FlatTypeKind.Bool:
                    return new BuiltInType(BuiltInTypeKind.Bool);
                case FlatTypeKind.Int:
                    return new BuiltInType(BuiltInTypeKind.Int);
                case FlatTypeKind.String:
                    return new BuiltInType(BuiltInTypeKind.String);
                case FlatTypeKind.Object:
                    return new BuiltInType(BuiltInTypeKind.Object);
                case FlatTypeKind.Array:
                    return new BuiltInType(BuiltInTypeKind.Array);
                case FlatTypeKind.ResourceRef:
                    return new BuiltInType(BuiltInTypeKind.ResourceRef);
                case FlatTypeKind.TypedObject:
                {
                    var name = flatType.Name ?? throw new ArgumentException(nameof(flatType));
                    var properties = flatType.Properties ?? throw new ArgumentException(nameof(flatType));
                    var concreteProperties = properties.ToDictionary(x => x.Key, x => GetReference(x.Value));
                    
                    return new ObjectType(name, concreteProperties);
                }
                case FlatTypeKind.TypedArray:
                {
                    var name = flatType.Name ?? throw new ArgumentException(nameof(flatType));
                    var item = flatType.Item ?? throw new ArgumentException(nameof(flatType));

                    return new ArrayType(name, GetReference(item));
                }
                case FlatTypeKind.Resource:
                {
                    var name = flatType.Name ?? throw new ArgumentException(nameof(flatType));
                    var properties = flatType.Properties ?? throw new ArgumentException(nameof(flatType));
                    var concreteProperties = properties.ToDictionary(x => x.Key, x => GetReference(x.Value));
                    
                    return new ResourceType(name, concreteProperties);
                }
                case FlatTypeKind.Union:
                {
                    var items = flatType.Items ?? throw new ArgumentException(nameof(flatType));
                    var concreteItems = items.Select(x => GetReference(x)).ToArray();
                    
                    return new UnionType(concreteItems);
                }
            }

            throw new ArgumentException(nameof(flatType));
        }
    }
}