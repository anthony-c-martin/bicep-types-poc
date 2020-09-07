using System.Collections.Generic;

namespace Bicep.Types.Concrete
{
    public class ObjectType : TypeBase
    {
        public string? Name { get; set; }

        public IDictionary<string, ITypeReference>? Properties { get; set; }

        public ITypeReference? AdditionalProperties { get; set; }
    }
}