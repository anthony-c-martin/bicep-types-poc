namespace Bicep.Types.Concrete
{
    public class ArrayType : TypeBase
    {
        public ArrayType(string name, ITypeReference itemType)
        {
            Name = name;
            ItemType = itemType;
        }

        public string Name { get; }

        public ITypeReference ItemType { get; }
    }
}