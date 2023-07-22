// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Extension methods for getting/setting properties on DynamicTableEntity's
    /// These are intended to be used wihin the table entity object classes to get/set values of specific types into DynamicTableEntity.Properties.
    /// </summary>
    public static class DynamicTableEntityExtensions
    {
        /// <summary>
        /// Sets the partition key
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="partitionKey">PartitionKey value</param>
        public static void SetPartitionKey(this DynamicTableEntity entity, string partitionKey)
        {
            entity.PartitionKey = partitionKey;
        }

        /// <summary>
        /// Sets the partition key
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="partitionKey">PartitionKey value</param>
        public static void SetPartitionKey(this DynamicTableEntity entity, long partitionKey)
        {
            entity.PartitionKey = partitionKey.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the partition key
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>PartitionKey value</returns>
        public static string GetPartitionKeyString(this DynamicTableEntity entity)
        {
            return entity.PartitionKey;
        }

        /// <summary>
        /// Gets the partition key as type long
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>PartitionKey value</returns>
        public static long GetPartitionKeyLong(this DynamicTableEntity entity)
        {
            return long.Parse(entity.PartitionKey, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Sets the RowKey to the qualified value
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="qualifier">Row type qualifier string</param>
        /// <param name="value">Row key (unqualified)</param>
        public static void SetQualifiedRowKey(this DynamicTableEntity entity, string qualifier, string value)
        {
            entity.RowKey = EntityFactory.QualifyRowKey(qualifier, value);
        }

        /// <summary>
        /// Sets the RowKey to the qualified value
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="qualifier">Row type qualifier string</param>
        /// <param name="value">Row key (unqualified)</param>
        public static void SetQualifiedRowKey(this DynamicTableEntity entity, string qualifier, long value)
        {
            entity.RowKey = EntityFactory.QualifyRowKey(qualifier, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets the unqualified rowkey value, if the qualifier matches (otherwise null)
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="qualifier">Row type qualifier string</param>
        /// <returns>Unqualified row key, or null</returns>
        public static string GetUnqualifiedRowKey(this DynamicTableEntity entity, string qualifier)
        {
            string unqualifiedKey;
            if (EntityFactory.TryGetQualifiedRowKey(entity.RowKey, qualifier, out unqualifiedKey))
            {
                return unqualifiedKey;
            }

            return null;
        }

        /// <summary>
        /// Gets the unqualified rowkey value, if the qualifier matches (otherwise null)
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="qualifier">Row type qualifier string</param>
        /// <param name="defaultValue">Default value if the field is not found</param>
        /// <returns>Unqualified row key as long, or null</returns>
        public static long GetUnqualifiedRowKeyLong(this DynamicTableEntity entity, string qualifier, long defaultValue = 0)
        {
            string unqualifiedKey = entity.GetUnqualifiedRowKey(qualifier);
            long value;
            if (!long.TryParse(unqualifiedKey, out value))
            {
                value = defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or default value</returns>
        public static string GetString(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.StringValue;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or empty value</returns>
        public static List<string> GetStringList(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                var value = property.StringValue;
                return
                    string.IsNullOrEmpty(value)
                    ? new List<string>()
                    : value.Split(',').Select(v => WebUtility.UrlDecode(v)).ToList();
            }

            return new List<string>();
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or empty value</returns>
        public static List<long> GetLongList(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                var value = property.StringValue;
                return
                    string.IsNullOrWhiteSpace(value)
                    ? new List<long>()
                    : value.Split(',').Select(v => long.Parse(WebUtility.UrlDecode(v), CultureInfo.InvariantCulture)).ToList();
            }

            return new List<long>();
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or null</returns>
        public static DateTimeOffset? GetDateTimeOffset(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.DateTimeOffsetValue;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or null</returns>
        public static DateTime? GetDateTime(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.DateTime;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or default value</returns>
        public static int? GetInt(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.Int32Value;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or null</returns>
        public static long? GetLong(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.Int64Value;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or default value</returns>
        public static Decimal? GetDecimal(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return Decimal.Parse(property.StringValue, CultureInfo.InvariantCulture);
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or null</returns>
        public static bool? GetBool(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.BooleanValue;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the specified field from this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <returns>Value in entity, or null</returns>
        public static Guid? GetGuid(this DynamicTableEntity entity, string field)
        {
            EntityProperty property;
            if (entity.Properties.TryGetValue(field, out property))
            {
                return property.GuidValue;
            }

            return null;
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, string value)
        {
            if (value != null)
            {
                entity[field] = EntityProperty.GeneratePropertyForString(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, List<string> value)
        {
            if (value != null)
            {
                entity[field] = EntityProperty.GeneratePropertyForString(string.Join(",", value.Select(v => WebUtility.UrlEncode(v))));
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }


        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, List<long> value)
        {
            if (value != null)
            {
                entity[field] = EntityProperty.GeneratePropertyForString(string.Join(",", value.Select(v => WebUtility.UrlEncode(v.ToString(CultureInfo.InvariantCulture)))));
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForDateTimeOffset(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, int? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForInt(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, long? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForLong(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, Decimal? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForString(value.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, bool? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForBool(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }

        /// <summary>
        /// Sets the value of the specified field for this entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="field">Field name</param>
        /// <param name="value">Value to set</param>
        public static void Set(this DynamicTableEntity entity, string field, Guid? value)
        {
            if (value.HasValue)
            {
                entity[field] = EntityProperty.GeneratePropertyForGuid(value);
            }
            else
            {
                if (entity.Properties.ContainsKey(field))
                {
                    entity.Properties.Remove(field);
                }
            }
        }
    }
}
