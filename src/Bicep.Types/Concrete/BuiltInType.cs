namespace Bicep.Types.Concrete
{
    public enum BuiltInTypeKind
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
    }

    public class BuiltInType : TypeBase
    {
        public BuiltInTypeKind? Kind { get; set; }
    }
}