using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bicep.Types.Serialized;
using System.IO;
using Bicep.Types.Concrete;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Bicep.Types.Tests
{
    public class FixedTypeReference : ITypeReference
    {
        public FixedTypeReference(TypeBase type)
        {
            Type = type;
        }

        public TypeBase Type { get; }
    }

    public class MutableTypeReference : ITypeReference
    {
        private TypeBase? mutableType;

        public TypeBase Type => mutableType ?? throw new InvalidOperationException($"{nameof(mutableType)} has not been initialized");

        public void Update(TypeBase type)
        {
            mutableType = type;
        }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var builtin = new BuiltInType(BuiltInTypeKind.Int);
            var a = new ArrayType("steve", new FixedTypeReference(builtin));
            var b = new ArrayType("ronald", new FixedTypeReference(builtin));

            var types = new TypeBase[] {
                builtin,
                a,
                b,
                new ObjectType("hi!", new Dictionary<string, ITypeReference>
                {
                    ["a"] = new FixedTypeReference(a),
                    ["b"] = new FixedTypeReference(b),
                }),
                new UnionType(new [] { new FixedTypeReference(a), new FixedTypeReference(b) }),
            };

            var json = TypeSerializer.Serialize(types);
            var newTypes = TypeSerializer.Deserialize(json);

            var json2 = TypeSerializer.Serialize(newTypes);

            json.Should().Equals(json2);
        }
    }
}
