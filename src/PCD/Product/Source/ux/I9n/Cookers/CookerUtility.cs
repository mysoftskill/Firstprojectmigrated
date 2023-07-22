using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class CookerUtility
    {
        private readonly IFixture fixture;

        public CookerUtility(IFixture iFixture)
        {
            fixture = iFixture;
        }

        public Guid GenerateFuzzyGuidFromName(string name)
        {
            return Guid.Parse(string.Format("{0:00000000-0000-0000-0000-000000000000}", GetNumberFromName(name)));
        }

        public int GetNumberFromName(string name)
        {
            var numberStr = Regex.Replace(name, @"[^\d]", "");
            return string.IsNullOrWhiteSpace(numberStr) ? 0 : Convert.ToInt32(numberStr);
        }

        public HttpResult CookHttpResultFor(string methodName)
        {
            return new HttpResult(HttpStatusCode.OK, null, null, HttpMethod.Get, null, null, 0L, methodName);
        }

        public Func<Task<IHttpResult>> CookEmptyHttpResult()
        {
            IHttpResult result = CookHttpResultFor("I9n_Method_Name");

            return () => {
                return Task.FromResult(result);
            };
        }

        public IList<T> CookListFrom<T>(IEnumerable<T> list)
        {
            return fixture.Build<List<T>>()
                          .Do(m => {
                              foreach (T item in list)
                              {
                                  m.Add(item);
                              }
                          })
                          .With(m => m.Capacity, list.Count())
                          .Create();
        }

        /// <summary>
        /// Creates an uninitialized object.
        /// This method should only be used for objects with readonly propeties that are internal/private.
        /// Otherwise please use `Fixture` based creation pattern. 
        /// </summary>
        public T CreateObjectWithReadonlyProperties<T>()
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }
    }
}
