using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoRest.AzureResourceSchema.Models;
using AutoRest.Core.Model;
using Bicep.Types.Concrete;

namespace AutoRest.AzureResourceSchema
{
    public class ProviderTypeGenerator
    {
        private readonly TypeFactory factory;
        private readonly IReadOnlyDictionary<BuiltInTypeKind, BuiltInType> builtInTypes;
        private readonly TypeBase apiVersionType;
        private readonly TypeBase dependsOnType;
        private readonly CodeModel codeModel;
        private readonly ProviderDefinition definition;
        private readonly IDictionary<string, TypeBase> namedDefinitions;

        public static GenerateResult Generate(CodeModel codeModel, ProviderDefinition definition)
        {
            var generator = new ProviderTypeGenerator(codeModel, definition);

            return generator.Process();
        }

        private ProviderTypeGenerator(CodeModel codeModel, ProviderDefinition definition)
        {
            factory = new TypeFactory(Enumerable.Empty<TypeBase>());
            this.builtInTypes = new Dictionary<BuiltInTypeKind, BuiltInType>
            {
                [BuiltInTypeKind.Any] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Any }),
                [BuiltInTypeKind.Null] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Null }),
                [BuiltInTypeKind.Bool] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Bool }),
                [BuiltInTypeKind.Int] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Int }),
                [BuiltInTypeKind.String] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.String }),
                [BuiltInTypeKind.Object] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Object }),
                [BuiltInTypeKind.Array] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.Array }),
                [BuiltInTypeKind.ResourceRef] = factory.Create(() => new BuiltInType { Kind = BuiltInTypeKind.ResourceRef }),
            };
            this.apiVersionType = factory.Create(() => new StringLiteralType { Value = definition.ApiVersion });
            this.dependsOnType = factory.Create(() => new ArrayType { ItemType = factory.GetReference(builtInTypes[BuiltInTypeKind.ResourceRef]) });
            this.codeModel = codeModel;
            this.definition = definition;
            this.namedDefinitions = new Dictionary<string, TypeBase>();
        }

        private ObjectProperty CreateObjectProperty(TypeBase type, ObjectPropertyFlags flags)
            => new ObjectProperty
            {
                Type = factory.GetReference(type),
                Flags = flags,
            };

        private Dictionary<string, ObjectProperty> GetStandardizedResourceProperties(ResourceDescriptor resourceDescriptor)
        {
            var type = factory.Create(() => new StringLiteralType { Value = resourceDescriptor.FullyQualifiedType });

            return new Dictionary<string, ObjectProperty>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = CreateObjectProperty(builtInTypes[BuiltInTypeKind.String], ObjectPropertyFlags.ReadOnly | ObjectPropertyFlags.DeployTimeConstant),
                ["name"] = CreateObjectProperty(builtInTypes[BuiltInTypeKind.String], ObjectPropertyFlags.Required | ObjectPropertyFlags.DeployTimeConstant),
                ["type"] = CreateObjectProperty(type, ObjectPropertyFlags.ReadOnly | ObjectPropertyFlags.DeployTimeConstant),
                ["apiVersion"] = CreateObjectProperty(apiVersionType, ObjectPropertyFlags.ReadOnly | ObjectPropertyFlags.DeployTimeConstant),
                ["dependsOn"] = CreateObjectProperty(dependsOnType, ObjectPropertyFlags.WriteOnly),
            };
        }

        private GenerateResult Process()
        {
            foreach (var resource in definition.ResourceDefinitions)
            {
                var descriptor = resource.Descriptor;
                var body = resource.DeclaringMethod.Body?.ModelType as CompositeType;

                var (success, failureReason, resourceName) = ParseNameSchema(resource, definition);
                if (!success)
                {
                    CodeModelProcessor.LogWarning($"Skipping resource type {descriptor.FullyQualifiedType} under path '{resource.DeclaringMethod.Url}': {failureReason}");
                    continue;
                }

                if (body == null)
                {
                    CodeModelProcessor.LogWarning($"Skipping resource type {descriptor.FullyQualifiedType} under path '{resource.DeclaringMethod.Url}': No resource body defined");
                    continue;
                }

                var resourceProperties = GetStandardizedResourceProperties(resource.Descriptor);
                var resourceDefinition = CreateObject(descriptor.FullyQualifiedType, body, resourceProperties);

                resource.Type = factory.Create(() => new ResourceType
                { 
                    Name = descriptor.FullyQualifiedType,
                    Body = factory.GetReference(resourceDefinition),
                });

                foreach (var property in body.ComposedProperties)
                {
                    if (property.SerializedName == null)
                    {
                        continue;
                    }

                    if (resourceProperties.ContainsKey(property.SerializedName))
                    {
                        continue;
                    }

                    var propertyDefinition = ParseType(property, property.ModelType);
                    if (propertyDefinition != null)
                    {
                        var flags = ParsePropertyFlags(property);
                        resourceProperties[property.SerializedName] = CreateObjectProperty(propertyDefinition, flags);
                    }
                }

                if (resourceDefinition is DiscriminatedObjectType discriminatedObjectType)
                {
                    HandlePolymorphicType(discriminatedObjectType, body);
                }
            }

            return new GenerateResult(
                definition.Namespace,
                definition.ApiVersion,
                factory,
                definition.ResourceDefinitions.Select(x => x.Descriptor));
        }

        private (bool success, string failureReason, TypeBase name) ParseNameSchema(ResourceDefinition resource, ProviderDefinition providerDefinition)
        {
            var finalProvidersMatch = CodeModelProcessor.parentScopePrefix.Match(resource.DeclaringMethod.Url);
            var routingScope = resource.DeclaringMethod.Url.Substring(finalProvidersMatch.Length);

            // get the resource name parameter, e.g. {fooName}
            var resNameParam = routingScope.Substring(routingScope.LastIndexOf('/') + 1);

            if (CodeModelProcessor.IsPathVariable(resNameParam))
            {
                // strip the enclosing braces
                resNameParam = CodeModelProcessor.TrimParamBraces(resNameParam);

                // look up the type
                var param = resource.DeclaringMethod.Parameters.FirstOrDefault(p => p.SerializedName == resNameParam);

                if (param == null)
                {
                    return (false, $"Unable to locate parameter with name '{resNameParam}'", null);
                }

                var nameType = ParseType(param.ClientProperty, param.ModelType);

                return (true, string.Empty, nameType);
            }

            if (!resNameParam.All(c => char.IsLetterOrDigit(c)))
            {
                return (false, $"Unable to process non-alphanumeric name '{resNameParam}'", null);
            }

            // Resource name is a constant
            return (true, string.Empty, CreateConstantResourceName(resource.Descriptor, resNameParam));
        }

        private ObjectPropertyFlags ParsePropertyFlags(Property property)
        {
            var flags = ObjectPropertyFlags.None;

            if (property.IsRequired)
            {
                flags |= ObjectPropertyFlags.Required;
            }

            if (property.IsReadOnly)
            {
                flags |= ObjectPropertyFlags.ReadOnly;
            }

            return flags;
        }

        private TypeBase ParseType(Property property, IModelType type)
        {
            // A schema that matches a JSON object with specific properties, such as
            // { "name": { "type": "string" }, "age": { "type": "number" } }
            if (type is CompositeType compositeType)
            {
                return ParseCompositeType(property, compositeType, true);
            } 
            // A schema that matches a "dictionary" JSON object, such as
            // { "additionalProperties": { "type": "string" } }
            if (type is DictionaryType dictionaryType)
            {
                return ParseDictionaryType(property, dictionaryType);
            }
            // A schema that matches a single value from a given set of values, such as
            // { "enum": [ "a", "b" ] }
            if (type is EnumType enumType)
            {
                return ParseEnumType(property, enumType);
            }
            // A schema that matches simple values, such as { "type": "number" }
            if (type is PrimaryType primaryType)
            {
                return ParsePrimaryType(primaryType);
            }
            // A schema that matches an array of values, such as
            // { "items": { "type": "number" } }
            if (type is SequenceType sequenceType)
            {
                return ParseSequenceType(property, sequenceType);
            }
            // A schema that matches anything
            if (type is MultiType)
            {
                return builtInTypes[BuiltInTypeKind.Any];
            }
            Debug.Fail("Unrecognized property type: " + type.GetType());

            return null;
        }

        private TypeBase CreateObject(string definitionName, CompositeType compositeType, Dictionary<string, ObjectProperty> properties)
        {
            if (compositeType.IsPolymorphic && compositeType.PolymorphicDiscriminator != null)
            {
                return factory.Create(() => new DiscriminatedObjectType
                {
                    Name = definitionName,
                    BaseProperties = properties,
                    Discriminator = compositeType.PolymorphicDiscriminator,
                    Elements = new Dictionary<string, ITypeReference>(),
                });
            }

            return factory.Create(() => new ObjectType
            {
                Name = definitionName,
                Properties = properties
            });
        }

        private TypeBase ParseCompositeType(Property property, CompositeType compositeType, bool includeBaseModelTypeProperties)
        {
            string definitionName = compositeType.Name;

            if (!namedDefinitions.ContainsKey(definitionName))
            {
                var definitionProperties = new Dictionary<string, ObjectProperty>();
                var definition = CreateObject(definitionName, compositeType, definitionProperties);

                // This definition must be added to the definition map before we start parsing
                // its properties because its properties may recursively reference back to this
                // definition.
                namedDefinitions.Add(definitionName, definition);

                var compositeTypeProperties = includeBaseModelTypeProperties ? compositeType.ComposedProperties : compositeType.Properties;
                foreach (var subProperty in compositeTypeProperties)
                {
                    var subPropertyDefinition = ParseType(subProperty, subProperty.ModelType);
                    if (subPropertyDefinition != null)
                    {
                        var flags = ParsePropertyFlags(subProperty);
                        definitionProperties[subProperty.SerializedName ?? subProperty.Name.RawValue] = CreateObjectProperty(subPropertyDefinition, flags);
                    }
                }

                if (definition is DiscriminatedObjectType discriminatedObjectType)
                {
                    HandlePolymorphicType(discriminatedObjectType, compositeType);
                }
            }

            return namedDefinitions[definitionName];
        }

        private TypeBase ParseDictionaryType(Property property, DictionaryType dictionaryType)
        {
            var additionalPropertiesType = ParseType(null, dictionaryType.ValueType);

            return factory.Create(() => new ObjectType
            {
                AdditionalProperties = factory.GetReference(additionalPropertiesType),
            });
        }

        private void HandlePolymorphicType(DiscriminatedObjectType discriminatedObjectType, CompositeType baseType)
        {
            foreach (var subType in codeModel.ModelTypes.Where(type => type.BaseModelType == baseType))
            {
                // Sub-types are never referenced directly in the Swagger
                // discriminator scenario, so they wouldn't be added to the
                // produced resource schema. By calling ParseCompositeType() on the
                // sub-type we add the sub-type to the resource schema.
                var polymorphicTypeRef = ParseCompositeType(null, subType, false);

                if (namedDefinitions[subType.Name] is ObjectType objectType)
                {
                    var discriminatorEnum = factory.Create(() => new StringLiteralType { Value = subType.SerializedName });
                    objectType.Properties[discriminatedObjectType.Discriminator] = new ObjectProperty
                    {
                        Type = factory.GetReference(discriminatorEnum),
                        Flags = ObjectPropertyFlags.Required,
                    };
                }

                discriminatedObjectType.Elements[subType.SerializedName] = factory.GetReference(polymorphicTypeRef);
            }
        }

        private TypeBase ParseEnumType(Property property, EnumType enumType)
        {
            var enumTypes = new List<TypeBase>();

            foreach (var enumValue in enumType.Values)
            {
                var stringLiteralType = factory.Create(() => new StringLiteralType { Value = enumValue.SerializedName });
                enumTypes.Add(stringLiteralType);
            }

            if (enumTypes.Count == 1)
            {
                return enumTypes.Single();
            }

            return factory.Create(() => new UnionType { Elements = enumTypes.Select(x => factory.GetReference(x)).ToArray() });
        }

        private TypeBase ParsePrimaryType(PrimaryType primaryType)
        {
            switch (primaryType.KnownPrimaryType)
            {
                case KnownPrimaryType.Boolean:
                    return builtInTypes[BuiltInTypeKind.Bool];
                case KnownPrimaryType.Int:
                case KnownPrimaryType.Long:
                case KnownPrimaryType.UnixTime:
                case KnownPrimaryType.Double:
                case KnownPrimaryType.Decimal:
                    return builtInTypes[BuiltInTypeKind.Int];
                case KnownPrimaryType.Object:
                    return builtInTypes[BuiltInTypeKind.Any];
                case KnownPrimaryType.ByteArray:
                    return builtInTypes[BuiltInTypeKind.Array];
                case KnownPrimaryType.Base64Url:
                case KnownPrimaryType.Date:
                case KnownPrimaryType.DateTime:
                case KnownPrimaryType.String:
                case KnownPrimaryType.TimeSpan:
                case KnownPrimaryType.Uuid:
                    return builtInTypes[BuiltInTypeKind.String];
                default:
                    Debug.Assert(false, "Unrecognized known property type: " + primaryType.KnownPrimaryType);
                    return builtInTypes[BuiltInTypeKind.Any];
            }
        }

        private TypeBase CreateConstantResourceName(ResourceDescriptor descriptor, string nameValue)
        {
            if (descriptor.IsRootType)
            {
                return factory.Create(() => new StringLiteralType { Value = nameValue });
            }

            return builtInTypes[BuiltInTypeKind.String];
        }

        private TypeBase ParseSequenceType(Property property, SequenceType sequenceType)
        {
            var itemType = ParseType(null, sequenceType.ElementType);

            if (itemType == null)
            {
                return builtInTypes[BuiltInTypeKind.Array];
            }

            return factory.Create(() => new ArrayType
            {
                ItemType = factory.GetReference(itemType),
            });
        }
    }
}