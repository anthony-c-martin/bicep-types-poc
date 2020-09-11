namespace Bicep.Types.Concrete
{
    public class ArrayType : TypeBase
    {
        public ITypeReference? ItemType { get; set; }
    }
}