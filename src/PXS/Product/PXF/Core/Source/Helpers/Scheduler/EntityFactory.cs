// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Creates entities from table results and associated helper methods
    /// </summary>
    internal static class EntityFactory
    {
        /// <summary>
        /// Adds a row key qualifier to distinguish what entity type is for a row
        /// </summary>
        /// <param name="qualifier">Qualifier string</param>
        /// <param name="value">Row key value (unqualified)</param>
        /// <returns>Qualified row key</returns>
        public static string QualifyRowKey(string qualifier, string value)
        {
            Assert(() => !qualifier.Contains(":"), "Qualifier may not contain a ':' character");
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", qualifier, value);
        }

        /// <summary>
        /// Attempts to parse out the unqualified row key from a qualified rowkey value
        /// </summary>
        /// <param name="qualifiedRowKey">Qualified row key</param>
        /// <param name="qualifier">Row qualifier</param>
        /// <param name="unqualifiedValue">outputs the unqualified value, if the qualifier matches</param>
        /// <returns>True if successful</returns>
        public static bool TryGetQualifiedRowKey(string qualifiedRowKey, string qualifier, out string unqualifiedValue)
        {
            unqualifiedValue = null;

            // If key is null or too small to have a deliminated value, fail quick.
            if (qualifiedRowKey == null || qualifiedRowKey.Length < 3)
            {
                return false;
            }

            var index = qualifiedRowKey.IndexOf(':');
            if (index < 0)
            {
                return false;
            }

            var left = qualifiedRowKey.Substring(0, index);
            if (left != qualifier)
            {
                return false;
            }

            unqualifiedValue = qualifiedRowKey.Substring(index + 1);
            return true;
        }

        /// <summary>
        /// Returns an entity of the specified type of the provided table result matches the rowQualifier
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="tableResult">TableResult row</param>
        /// <param name="rowQualifier">Row qualifier</param>
        /// <returns>Entity, if qualifier matches</returns>
        public static TEntity FromTableResult<TEntity>(TableResult tableResult, string rowQualifier)
            where TEntity : TableEntityBase, new()
        {
            if (tableResult.Result == null)
            {
                return null;
            }

            // Make sure this is the right type of row
            var result = (DynamicTableEntity)tableResult.Result;
            if (result.GetUnqualifiedRowKey(rowQualifier) == null)
            {
                return null;
            }

            var row = new TEntity();
            row.Entity = result;
            return row;
        }

        /// <summary>
        /// Casts all entities to the specified type
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="rows">Result rows</param>
        /// <returns>A list of entities</returns>
        public static IList<TEntity> FromDynamicTableEntities<TEntity>(IEnumerable<DynamicTableEntity> rows)
            where TEntity : TableEntityBase, new()
        {
            var entities = new List<TEntity>();
            foreach (var row in rows)
            {
                var entity = new TEntity();
                entity.Entity = row;
                entities.Add(entity);
            }

            return entities;
        }

        /// <summary>
        /// Returns a set of entities of the specified type for any rows that match the row qualifer
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="rows">Result rows</param>
        /// <param name="rowQualifier">Row qualifier</param>
        /// <returns>A list of entities that match (could be empty list)</returns>
        public static IList<TEntity> FromDynamicTableEntities<TEntity>(IEnumerable<DynamicTableEntity> rows, string rowQualifier)
            where TEntity : TableEntityBase, new()
        {
            var entities = new List<TEntity>();
            foreach (var row in rows)
            {
                string unqualifiedRowKey;
                if (EntityFactory.TryGetQualifiedRowKey(row.RowKey, rowQualifier, out unqualifiedRowKey))
                {
                    var entity = new TEntity();
                    entity.Entity = row;
                    entities.Add(entity);
                }
            }

            return entities;
        }

        [Conditional("DEBUG")]
        private static void Assert(Func<bool> condition, string message)
        {
            if (!condition())
            {
                throw new NotSupportedException(message);
            }
        }
    }
}
