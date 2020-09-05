using System;
using System.Collections.Generic;
using Bicep.Types.Serialized;

namespace Bicep.Types.Concrete
{
    public class TypeFactory
    {
        private readonly List<TypeBase> allocatedTypes;

        private class AllocatedTypeReference : ITypeReference
        {
            private readonly TypeFactory factory;
            private readonly int index;

            public AllocatedTypeReference(int index, TypeFactory factory)
            {
                this.index = index;
                this.factory = factory;
            }

            public TypeBase Type => factory.allocatedTypes[index];
        }

        private TypeFactory()
        {
            allocatedTypes = new List<TypeBase>();
        }

        public static TypeBase[] FromSerialized(FlatType[] flatTypes)
        {
            var factory = new TypeFactory();

            return factory.allocatedTypes.ToArray();
        }

        public static FlatType[] ToSerialized(TypeBase[] types)
        {
            return Array.Empty<FlatType>();
        }
    }
}