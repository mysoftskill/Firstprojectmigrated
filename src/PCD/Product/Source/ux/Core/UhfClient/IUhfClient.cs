using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Models.Uhf;

namespace Microsoft.PrivacyServices.UX.Core.UhfClient
{
    public interface IUhfClient
    {
        /// <summary>
        /// Loads a UHF shell model to inject our header, footer 
        /// and any other necessary information for the UHF
        /// </summary>
        Task<Uhf> LoadUhfModel(string cultureCode);
    }
}
