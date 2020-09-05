using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bicep.Types.Serialized;
using System.IO;

namespace Bicep.Types.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var a = new ArrayType();
            var b = new ArrayType();
            a.ItemType = b;
            b.ItemType = a;

            var types = new TypeBase[] {
                a,
                b,
                new ObjectType(),
                new UnionType(),
            };

            using (var testStream = new MemoryStream())
            {
                TypeSerializer.Serialize(testStream, types);

                testStream.Position = 0;
                var reader = new StreamReader(testStream);
                var text = reader.ReadToEnd();

                testStream.Position = 0;
                var output = TypeSerializer.Deserialize(testStream);
            }
        }
    }
}
