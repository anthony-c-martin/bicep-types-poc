using System.Collections.Generic;

namespace Bicep.Types.Concrete
{
    public class ResourceType : TypeBase
    {
        public ResourceType(string name, IDictionary<string, ITypeReference> properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }
        
        public IDictionary<string, ITypeReference> Properties { get; }
    }
}