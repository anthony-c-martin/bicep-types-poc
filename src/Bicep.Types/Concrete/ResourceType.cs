using System.Collections.Generic;

namespace Bicep.Types.Concrete
{
    public class ResourceType : TypeBase
    {
        public string? Name { get; set; }
        
        public ITypeReference? Body { get; set; }
    }
}