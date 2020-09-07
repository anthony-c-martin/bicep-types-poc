using Bicep.Types.Concrete;

namespace Bicep.Types.Serialized
{
    public interface ITypeReferenceResolver
    {
        int GetIndex(TypeBase type);

        ITypeReference GetTypeReference(int index);
    }
}