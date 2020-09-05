using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bicep.Types.Concrete;
using System;
using System.Collections.Generic;
using FluentAssertions;
using Bicep.Types.Arm;
using System.Linq;

namespace Bicep.Types.Tests
{
    [TestClass]
    public class SerializationTests
    {
        private class FixedTypeReference : ITypeReference
        {
            public FixedTypeReference(TypeBase type)
            {
                Type = type;
            }

            public TypeBase Type { get; }
        }

        private class MutableTypeReference : ITypeReference
        {
            private TypeBase? mutableType;

            public TypeBase Type => mutableType ?? throw new InvalidOperationException($"{nameof(mutableType)} has not been initialized");

            public void Update(TypeBase type)
            {
                mutableType = type;
            }
        }

        [TestMethod]
        public void Serialize_Deserialize()
        {
            var builtin = new BuiltInType { Kind = BuiltInTypeKind.Int };
            var a = new ArrayType { Name = "steve", ItemType = new FixedTypeReference(builtin) };
            var b = new ArrayType { Name = "ronald", ItemType = new FixedTypeReference(builtin) };

            var mutableRef = new MutableTypeReference();
            var cyclicArray = new ArrayType { Name = "cyclic", ItemType = mutableRef };
            mutableRef.Update(cyclicArray);

            var types = new TypeBase[] {
                builtin,
                a,
                b,
                new ObjectType
                {
                    Name = "hi!", Properties = new Dictionary<string, ITypeReference>
                    {
                        ["a"] = new FixedTypeReference(a),
                        ["b"] = new FixedTypeReference(b),
                    },
                },
                new UnionType
                { 
                    Elements = new [] { new FixedTypeReference(a), new FixedTypeReference(b) },
                },
                new StringLiteralType { Value = "abc" },
                cyclicArray,
            };

            var serialized = TypeSerializer.Serialize(types);
            var deserialized = TypeSerializer.Deserialize(serialized);

            serialized.Should().Equals(TypeSerializer.Serialize(deserialized));
        }
    }
}
