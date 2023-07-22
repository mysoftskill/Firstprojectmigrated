// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    /// <summary>
    /// A wrapper around generic actions you can do to a config
    /// </summary>
    public static class ConfigurationUtility
    {
        /// <summary>
        /// Given the inital config object (or any object in the config) will search
        /// through it and its children to find all members specified, iteratively
        /// </summary>
        /// <typeparam name="T">The type to look in</typeparam>
        /// <typeparam name="T1">The type to find</typeparam>
        /// <param name="startingObject">The starting object to look in of type T</param>
        /// <returns>A list of all T1s found</returns>
        public static IEnumerable<T1> GetAllOfATypeInAnotherTypeIteratively<T, T1>(T startingObject)
        {
            //items to return
            List<T1> itemsToReturn = new List<T1>();

            //list of items to search in
            Queue<object> listOfItemsToSearchThrough = new Queue<object>();

            //if the object is null, just return
            if (startingObject == null)
            {
                return itemsToReturn;
            }

            //the item to look into
            object item = null;

            //add the item to start with to the queue.
            listOfItemsToSearchThrough.Enqueue(startingObject);

            //while there are items to investigate
            while (listOfItemsToSearchThrough.Count > 0)
            {
                //take the item to look at.
                item = listOfItemsToSearchThrough.Dequeue();

                //if the item is null, just move on
                if (item == null)
                {
                    continue;
                }

                //reflect over the item
                foreach (var property in item.GetType().GetProperties())
                {
                    //if the property is a list of T1s then let us add it to the return
                    if (property.PropertyType == typeof(List<T1>))
                    {
                        List<T1> value = (List<T1>)property.GetValue(item);
                        if (value != null)
                        {
                            itemsToReturn.AddRange(value);
                        }
                    }
                    else if (property.PropertyType == typeof(IList<T1>))
                    {
                        //if the property is a list of T1s let us add it to the return
                        IList<T1> value = (IList<T1>)property.GetValue(item);
                        if (value != null)
                        {
                            itemsToReturn.AddRange(value);
                        }
                    }
                    else if (property.PropertyType == typeof(T1))
                    {
                        //if property is a T1 let us add it to the return
                        T1 value = (T1)property.GetValue(item);
                        if (value != null)
                        {
                            itemsToReturn.Add(value);
                        }
                    }
                    else if (property.PropertyType.IsValueType)
                    {
                        //if property is of a value type: string, int, etc. And has not been added yet, move on.
                        continue;
                    }
                    else
                    {
                        //if the type is not found, and it is not a value type, enqueue to be looked at
                        listOfItemsToSearchThrough.Enqueue((property.GetValue(item)));
                    }
                }
            }

            //return all the items
            return itemsToReturn;
        }

    }
}
