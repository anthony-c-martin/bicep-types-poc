using AutoRest.Core.Model;
using Bicep.Types.Concrete;

namespace AutoRest.AzureResourceSchema.Models
{
    public class ResourceDefinition
    {
        public ResourceDescriptor Descriptor { get; set; }

        public Method DeclaringMethod { get; set; }

        public Method GetMethod { get; set; }

        public ResourceType Type { get; set; }
    }
}