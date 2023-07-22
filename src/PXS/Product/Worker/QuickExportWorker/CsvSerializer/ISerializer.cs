namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

	public interface ISerializer
	{
		/// <summary>
		/// Serializes the Resource returned from PDOS into a readable file-format
		/// </summary>
		/// <param name="fields">A list of strings representing the entries in the file</param>
		/// <returns>A string representing the file header</returns>
		string WriteHeader(IEnumerable<string> fields);
		/// <summary>
		///  Converts the rest of the resource into the desired ouptu
		/// </summary>
		/// <param name="resource">PDOS resource</param>
		/// <returns>A string of the converted resource</returns>
		string ConvertResource(Resource resource);
	}
}
