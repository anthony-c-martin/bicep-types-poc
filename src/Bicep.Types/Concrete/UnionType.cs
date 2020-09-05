namespace Bicep.Types.Concrete
{
    public class UnionType : TypeBase
    {
        public ITypeReference[]? Elements { get; set; }
    }
}