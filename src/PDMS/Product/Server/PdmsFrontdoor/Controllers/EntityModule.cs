namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http.ModelBinding;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;

    /// <summary>
    /// Helper class to get, update or create entity.
    /// </summary>
    public static class EntityModule
    {
        /// <summary>
        /// Create entity.
        /// </summary>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="value">The entity object.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Delegate to create entity.</param>
        /// <returns>The API entity object returned from the operation.</returns>
        public static async Task<TApi> CreateAsync<TCore, TApi>(
            ModelStateDictionary modelState,
            TApi value,
            IMapper autoMapper,
            Func<TCore, Task<TCore>> entityOperation)
        {
            if (value == null)
            {
                throw new Exceptions.InvalidArgumentError(typeof(TApi).ToString(), "null");
            }

            return await ExecuteAsync(modelState, value, autoMapper, entityOperation).ConfigureAwait(false);
        }

        /// <summary>
        /// Update entity.
        /// </summary>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <param name="key">The Id of the entity.</param>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="value">The entity object.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Delegate to update entity.</param>
        /// <returns>The API entity object returned from the operation.</returns>
        public static async Task<TApi> UpdateAsync<TCore, TApi>(
            string key,
            ModelStateDictionary modelState,
            TApi value,
            IMapper autoMapper,
            Func<TCore, Task<TCore>> entityOperation)
            where TApi : Entity
        {
            if (value == null)
            {
                throw new Exceptions.InvalidArgumentError(typeof(TApi).ToString(), "null");
            }

            // Perform id check after model state is validated.
            Func<TCore, Task<TCore>> operation = core =>
            {
                if (key != value.Id)
                {
                    string message = $"The key {key} and Id in Entity {value.Id} don't match.";
                    throw new InvalidArgumentError(nameof(key), message);
                }

                return entityOperation(core);
            };

            return await ExecuteAsync(modelState, value, autoMapper, operation).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete entity.
        /// </summary>
        /// <param name="key">The Id of the entity.</param>
        /// <param name="etag">The ETag of the resource.</param>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Delegate to delete entity.</param>
        /// <returns>The task that executes the operation.</returns>
        public static async Task DeleteAsync(
            string key,
            string etag,
            ModelStateDictionary modelState,
            IMapper autoMapper,
            Func<Guid, string, Task> entityOperation)
        {
            Func<Task> operation = async () =>
            {
                if (!modelState.IsValid)
                {
                    throw new InvalidModelError(modelState);
                }

                if (string.IsNullOrWhiteSpace(etag))
                {
                    throw new MissingPropertyException("ETag", "Empty ETag");
                }

                Guid id;
                if (Guid.TryParse(key, out id))
                {
                    await entityOperation(id, etag).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidArgumentError("id", key);
                }
            };

            try
            {
                await operation().ConfigureAwait(false);
            }
            catch (CoreException coreException)
            {
                throw autoMapper.Map<ServiceException>(coreException);
            }
        }

        /// <summary>
        /// Delete entity.
        /// </summary>
        /// <param name="key">The Id of the entity.</param>
        /// <param name="etag">The ETag of the resource.</param>
        /// <param name="overridePendingCommandsCheck">Override pending commands check.</param>
        /// <param name="forceDelete">Force delete flag.</param>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Delegate to delete entity.</param>
        /// <returns>The task that executes the operation.</returns>
        public static async Task DeleteAsync(
            string key,
            string etag,
            bool overridePendingCommandsCheck,
            bool forceDelete,
            ModelStateDictionary modelState,
            IMapper autoMapper,
            Func<Guid, string, bool, bool, Task> entityOperation)
        {
            Func<Task> operation = async () =>
            {
                if (!modelState.IsValid)
                {
                    throw new InvalidModelError(modelState);
                }

                if (string.IsNullOrWhiteSpace(etag))
                {
                    throw new MissingPropertyException("ETag", "Empty ETag");
                }

                Guid id;
                if (Guid.TryParse(key, out id))
                {
                    await entityOperation(id, etag, overridePendingCommandsCheck, forceDelete).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidArgumentError("id", key);
                }
            };

            try
            {
                await operation().ConfigureAwait(false);
            }
            catch (CoreException coreException)
            {
                throw autoMapper.Map<ServiceException>(coreException);
            }
        }

        /// <summary>
        /// Get entity.
        /// </summary>
        /// <typeparam name="U">The Core entity type.</typeparam>
        /// <typeparam name="V">The API entity type.</typeparam>
        /// <param name="key">The Id of the entity.</param>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="options">The OData query options.</param>
        /// <param name="entityOperation">Delegate to get entity.</param>
        /// <returns>The Core entity object returned from the operation.</returns>
        public static async Task<V> GetAsync<U, V>(
            string key,
            ModelStateDictionary modelState,
            IMapper autoMapper,
            ODataQueryOptions<V> options,
            Func<Guid, Task<U>> entityOperation)
        {
            Func<Task<U>> operation = async () =>
            {
                Guid id;

                if (Guid.TryParse(key, out id))
                {
                    var coreEntityResponse = await entityOperation(id).ConfigureAwait(false);

                    if (coreEntityResponse == null)
                    {
                        throw new EntityNotFoundError(id, typeof(V).Name);
                    }

                    return coreEntityResponse;
                }
                else
                {
                    throw new InvalidArgumentError("id", key);
                }
            };

            V entityResponse = await ExecuteAsync<U, V>(modelState, autoMapper, operation).ConfigureAwait(false);

            if (options?.SelectExpand != null)
            {
                options.Request.ODataProperties().SelectExpandClause = options.SelectExpand.SelectExpandClause;
            }

            return entityResponse;
        }

        /// <summary>
        /// Get all entities.
        /// </summary>
        /// <typeparam name="T">The entity filter criteria type.</typeparam>
        /// <typeparam name="U">The Core entity type.</typeparam>
        /// <typeparam name="V">The API entity type.</typeparam>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="autoMapper">An instance of AutoMapper.</param>
        /// <param name="request">The current request.</param>
        /// <param name="options">The OData query options.</param>
        /// <param name="filterCriteria">The filter criteria.</param>
        /// <param name="entityOperation">Delegate to get all entities.</param>
        /// <returns>The final page result for returning to the caller.</returns>
        public static async Task<PageResult<V>> GetAllAsync<T, U, V>(
            ModelStateDictionary modelState,
            IMapper autoMapper,
            HttpRequestMessage request,
            ODataQueryOptions<V> options,
            T filterCriteria,
            Func<T, Task<FilterResult<U>>> entityOperation)
        {
            FilterResult<U> coreEntityResponse = null;

            Func<Task<IEnumerable<U>>> operation = async () =>
            {
                coreEntityResponse = await entityOperation(filterCriteria).ConfigureAwait(false);
                return coreEntityResponse.Values;
            };

            var entities = await ExecuteAsync<IEnumerable<U>, IEnumerable<V>>(modelState, autoMapper, operation).ConfigureAwait(false);

            var nextSkip = coreEntityResponse.Index + coreEntityResponse.Count;
            Uri nextLink = null;

            // Build the next link for server side paging. 
            // We need to preserve the filter if it exists.
            if (nextSkip < coreEntityResponse.Total)
            {
                var uri = $"https://{request.RequestUri.DnsSafeHost}{request.RequestUri.AbsolutePath}?";

                if (options.SelectExpand != null)
                {
                    if (options.SelectExpand.RawSelect != null)
                    {
                        uri += $"$select={options.SelectExpand.RawSelect}&";
                    }

                    if (options.SelectExpand.RawExpand != null)
                    {
                        uri += $"$expand={options.SelectExpand.RawExpand}&";
                    }
                }

                if (options.Filter != null)
                {
                    uri += $"$filter={options.Filter.RawValue}&";
                }

                uri += $"$top={coreEntityResponse.Count}&$skip={nextSkip}";

                nextLink = new Uri(uri);
            }

            if (options.SelectExpand != null)
            {
                options.Request.ODataProperties().SelectExpandClause = options.SelectExpand.SelectExpandClause;
            }

            return new PageResult<V>(entities, nextLink, coreEntityResponse.Total);
        }

        /// <summary>
        /// Execute a method that receives and returns a Core type.
        /// </summary>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="value">The entity object.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Create or update operation.</param>
        /// <returns>The API entity object returned from the operation.</returns>
        public static Task<TApi> ExecuteAsync<TCore, TApi>(
            ModelStateDictionary modelState,
            TApi value,
            IMapper autoMapper,
            Func<TCore, Task<TCore>> entityOperation)
        {
            // This approach is so that the ModelState check occurs before using the model data.
            Func<Task<TCore>> operation = () =>
            {
                TCore coreEntity = Map<TApi, TCore>(autoMapper, value);
                return entityOperation(coreEntity);
            };

            return ExecuteAsync<TCore, TApi>(modelState, autoMapper, operation);
        }

        /// <summary>
        /// Execute a void method that returns a Core type.
        /// </summary>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <param name="modelState">The web API model state.</param>
        /// <param name="autoMapper">AutoMapper instance.</param>
        /// <param name="entityOperation">Create or update operation.</param>
        /// <returns>The API entity object returned from the operation.</returns>
        public static async Task<TApi> ExecuteAsync<TCore, TApi>(
            ModelStateDictionary modelState,
            IMapper autoMapper,
            Func<Task<TCore>> entityOperation)
        {
            if (!modelState.IsValid)
            {
                throw new InvalidModelError(modelState);
            }

            try
            {
                var coreResponse = await entityOperation().ConfigureAwait(false);

                return Map<TCore, TApi>(autoMapper, coreResponse);
            }
            catch (CoreException coreException)
            {
                throw autoMapper.Map<ServiceException>(coreException);
            }
        }

        /// <summary>
        /// Maps a business layer response into the API response
        /// and handles any exception conversions.
        /// </summary>
        /// <typeparam name="TCore">The core data type.</typeparam>
        /// <typeparam name="TApi">The API data type.</typeparam>
        /// <param name="autoMapper">The mapper object.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The result.</returns>
        public static TApi Map<TCore, TApi>(IMapper autoMapper, TCore value)
        {
            try
            {
                return autoMapper.Map<TCore, TApi>(value);
            }
            catch (AutoMapperMappingException ex)
            {
                Exception innerEx = ex.InnerException;

                while (innerEx != null)
                {
                    if (innerEx is ServiceException)
                    {
                        throw innerEx;
                    }
                    else
                    {
                        innerEx = innerEx.InnerException;
                    }
                }

                throw;
            }
        }
    }
}
