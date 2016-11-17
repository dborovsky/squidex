﻿// ==========================================================================
//  SchemaCommandHandler.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Squidex.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.CQRS.Commands;
using Squidex.Infrastructure.Dispatching;
using Squidex.Read.Schemas.Services;
using Squidex.Write.Schemas.Commands;

namespace Squidex.Write.Schemas
{
    public class SchemaCommandHandler : CommandHandler<SchemaDomainObject>
    {
        private readonly FieldRegistry registry;
        private readonly ISchemaProvider schemaProvider;
        private readonly JsonSerializer serializer;

        public SchemaCommandHandler(
            FieldRegistry registry,
            ISchemaProvider schemaProvider,
            IDomainObjectFactory domainObjectFactory,
            IDomainObjectRepository domainObjectRepository,
            JsonSerializer serializer)
            : base(domainObjectFactory, domainObjectRepository)
        {
            this.registry = registry;
            this.serializer = serializer;
            this.schemaProvider = schemaProvider;
        }

        public override Task<bool> HandleAsync(CommandContext context)
        {
            return context.IsHandled ? Task.FromResult(false) : this.DispatchActionAsync(context.Command);
        }

        public async Task On(CreateSchema command)
        {
            if (await schemaProvider.FindSchemaIdByNameAsync(command.AppId, command.Name) != null)
            {
                var error = new ValidationError($"A schema with name '{command.Name}' already exists", "DisplayName");

                throw new ValidationException("Cannot create a new schema", error);
            }
            await CreateAsync(command, s => s.Create(command));
        }

        public Task On(DeleteSchema command)
        {
            return UpdateAsync(command, s => s.Delete());
        }

        public Task On(DeleteField command)
        {
            return UpdateAsync(command, s => s.DeleteField(command.FieldId));
        }

        public Task On(DisableField command)
        {
            return UpdateAsync(command, s => s.DisableField(command.FieldId));
        }

        public Task On(EnableField command)
        {
            return UpdateAsync(command, s => s.EnableField(command.FieldId));
        }

        public Task On(HideField command)
        {
            return UpdateAsync(command, s => s.HideField(command.FieldId));
        }

        public Task On(ShowField command)
        {
            return UpdateAsync(command, s => s.ShowField(command.FieldId));
        }

        public Task On(UpdateSchema command)
        {
            return UpdateAsync(command, s => s.Update(command));
        }

        public Task On(AddField command)
        {
            var propertiesType = registry.FindByTypeName(command.Type).PropertiesType;
            var propertiesValue = CreateProperties(command.Properties, propertiesType);

            return UpdateAsync(command, s => s.AddField(command, propertiesValue));
        }

        public Task On(UpdateField command)
        {
            return UpdateAsync(command, s =>
            {
                var field = s.Schema.Fields.GetOrDefault(command.FieldId);

                if (field == null)
                {
                    throw new DomainObjectNotFoundException(command.FieldId.ToString(), typeof(Field));
                }

                var propertiesType = registry.FindByPropertiesType(field.RawProperties.GetType()).PropertiesType;
                var propertiesValue = CreateProperties(command.Properties, propertiesType);

                s.UpdateField(command, propertiesValue);
            });
        }

        private FieldProperties CreateProperties(JToken token, Type type)
        {
            return (FieldProperties)token.ToObject(type, serializer);
        }
    }
}
