namespace Bicep.Types.Concrete
{
    public class UnionType : TypeBase
    {
        public UnionType(ITypeReference[] elements)
        {
            Elements = elements;
        }

        public ITypeReference[] Elements { get; }
    }
}