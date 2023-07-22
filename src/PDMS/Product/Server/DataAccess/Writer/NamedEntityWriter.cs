namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Base class for all named entity write operations.
    /// </summary>
    /// <typeparam name="TNamedEntity">The named entity type.</typeparam>
    /// <typeparam name="TFilterCriteria">The filter criteria type.</typeparam>
    public abstract class NamedEntityWriter<TNamedEntity, TFilterCriteria> : EntityWriter<TNamedEntity>
         where TNamedEntity : NamedEntity
         where TFilterCriteria : NamedEntityFilterCriteria<TNamedEntity>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedEntityWriter{TNamedEntity, TFilterCriteria}" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        protected NamedEntityWriter(
            IPrivacyDataStorageWriter storageWriter,
            IEntityReader<TNamedEntity> entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IEventWriterFactory eventWriterFactory = null)
            : base(
                  storageWriter,
                  entityReader, 
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper,
                  eventWriterFactory)
        {
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, TNamedEntity incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            ValidationModule.PropertyRequired(incomingEntity.Name, "name");
            ValidationModule.StringTooLong("name", incomingEntity.Name, 128);
            ValidationModule.StringContainNotAllowedCharacters("name", incomingEntity.Name);
            ValidationModule.StringMustContainOnlyASCIICharacters("name", incomingEntity.Name);
            ValidationModule.PropertyRequired(incomingEntity.Description, "description");
            ValidationModule.StringTooLong("description", incomingEntity.Description, 1024);
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, TNamedEntity incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (action == WriteAction.Create)
            {
                await this.EntityNameShouldBeUnique(incomingEntity).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (incomingEntity.Name != existingEntity.Name)
                {
                    await this.EntityNameShouldBeUnique(incomingEntity).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Validate if ICM contains empty guid.
        /// </summary>
        /// <param name="icm">ICM object.</param>
        protected void ValidateIcm(Icm icm)
        {
            if (icm != null)
            {
                if (icm.ConnectorId == Guid.Empty)
                {
                    throw new InvalidPropertyException("connectorId", icm.ConnectorId.ToString(), "Empty Guid for ConnectorId is not allowed.");
                }
                else if (icm.Source == IcmSource.Manual && icm.TenantId != default(long))
                {
                    throw new InvalidPropertyException("tenantId", icm.TenantId.ToString(), "TenantId must be empty for Manual source.");
                }
                else if (icm.Source == IcmSource.ServiceTree && icm.TenantId == default(long))
                {
                    throw new InvalidPropertyException("tenantId", icm.TenantId.ToString(), "TenantId must be set for ServiceTree source.");
                }
            }
        }

        /// <summary>
        /// Helper function to check if the name of the incomingEntity is unique in PDMS collection.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task EntityNameShouldBeUnique(TNamedEntity incomingEntity)
        {
            var filterCriteria = new TFilterCriteria { Name = new StringFilter(incomingEntity.Name) };

            var existingEntities = await this.EntityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            if (existingEntities.Values.Any())
            {
                throw new ConflictException(ConflictType.AlreadyExists, "The name is already in use.", "name", incomingEntity.Name);
            }
        }
    }
}