using System.Collections.Generic;

namespace Bicep.Types.Serialized
{
    public enum FlatTypeKind
    {
        Never = 1,
        Any = 2,
        Null = 3,
        Bool = 4,
        Int = 5,
        String = 6,
        Object = 7,
        Array = 8,
        ResourceRef = 9,
        TypedObject = 10,
        TypedArray = 11,
        Resource = 12,
        Union = 13,
    }

    public class FlatType
    {
        public int? Index { get; set; }

        public FlatTypeKind? Kind { get; set; }

        public IDictionary<string, int>? Properties { get; set; }

        public IList<int>? Items { get; set; }

        public string? Name { get; set; }

        public int? Item { get; set; }
    }
}