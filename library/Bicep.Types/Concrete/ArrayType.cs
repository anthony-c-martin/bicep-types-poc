namespace Bicep.Types.Concrete
{
    public class ArrayType : TypeBase
    {
        public string? Name { get; set; }

        public ITypeReference? ItemType { get; set; }
    }
}