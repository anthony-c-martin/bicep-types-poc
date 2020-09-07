using System.Collections.Generic;

namespace Bicep.Types.Concrete
{
    public class DiscriminatedObjectType : TypeBase
    {
        public string? Name { get; set; }

        public string? Discriminator { get; set; }

        public IDictionary<string, ITypeReference>? BaseProperties { get; set; }

        public IDictionary<string, ITypeReference>? Elements { get; set; }
    }
}