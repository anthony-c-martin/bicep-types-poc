using System;
using System.Collections.Generic;
using System.Linq;
using Bicep.Types.Concrete;

namespace Bicep.Types.Serialized
{
    public class ToFlatTypeConverter
    {
        private IReadOnlyDictionary<TypeBase, int> concreteTypes;

        public ToFlatTypeConverter(IReadOnlyList<TypeBase> concreteTypes)
        {
            this.concreteTypes = Enumerable.Range(0, concreteTypes.Count).ToDictionary(x => concreteTypes[x], x => x);
        }

        public FlatType Convert(TypeBase concreteType)
        {
            var flatType = new FlatType
            {
                Index = concreteTypes[concreteType],
            };

            switch (concreteType)
            {
                case BuiltInType builtInType:
                    flatType.Kind = builtInType.Kind switch {
                        BuiltInTypeKind.Never => FlatTypeKind.Never,
                        BuiltInTypeKind.Any => FlatTypeKind.Any,
                        BuiltInTypeKind.Null => FlatTypeKind.Null,
                        BuiltInTypeKind.Bool => FlatTypeKind.Bool,
                        BuiltInTypeKind.Int => FlatTypeKind.Int,
                        BuiltInTypeKind.String => FlatTypeKind.String,
                        BuiltInTypeKind.Object => FlatTypeKind.Object,
                        BuiltInTypeKind.Array => FlatTypeKind.Array,
                        BuiltInTypeKind.ResourceRef => FlatTypeKind.ResourceRef,
                        _ => throw new ArgumentException(nameof(concreteType)),
                    };
                    return flatType;
                case ObjectType objectType:
                    flatType.Kind = FlatTypeKind.TypedObject;
                    flatType.Name = objectType.Name;
                    flatType.Properties = objectType.Properties.ToDictionary(x => x.Key, x => concreteTypes[x.Value.Type]);
                    return flatType;
                case ArrayType arrayType:
                    flatType.Kind = FlatTypeKind.TypedArray;
                    flatType.Name = arrayType.Name;
                    flatType.Item = concreteTypes[arrayType.ItemType.Type];
                    return flatType;
                case ResourceType resourceType:
                    flatType.Kind = FlatTypeKind.Resource;
                    flatType.Name = resourceType.Name;
                    flatType.Properties = resourceType.Properties.ToDictionary(x => x.Key, x => concreteTypes[x.Value.Type]);
                    return flatType;
                case UnionType unionType:
                    flatType.Kind = FlatTypeKind.Union;
                    flatType.Items = unionType.Elements.Select(x => concreteTypes[x.Type]).ToList();
                    return flatType;
            }

            throw new ArgumentException(nameof(concreteType));
        }
    }
}