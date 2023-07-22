using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Identity;
using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GetPcfCommands
{

    /// <summary>
    /// Read a list of command ids from a file and query the ComandHistory database for details
    // Arguments:
    //      args[0] - the file containing the commands (required)
    //      		Format: one command id (guid) per line; guid can contain dashes or not
    //      args[1] - an ini file with the database endpoint url and primary key (defaults to .\CosmosDbConfig.ini
    //                  Format:
    //                  EndpointUrl=https://....
    //                  PrimaryKey=<encrypted key>
    // Output:
    ///     commandsFound.txt - the list of commands that were successfully found
    ///     commandsDetails.json - the json output with the details from the c.pxs element
    /// </summary>
    class Program
    {
        // Left here for reference
        private static readonly string EndpointUrl_PPE = "https://pcf-ppe-coldstorage.documents.azure.com:443/";
        private static readonly string EndpointUrl_Prod = "https://pcf-prod-commandhistory.documents.azure.com:443/";

        private static string EndpointUrl;
        private static string PrimaryKey;

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database
        private Database database;

        // The container
        private Container container;

        // The name of the database and container we will create
        private string databaseName = "CommandHistoryDb";
        private string containerName = "CommandHistory";

        public static async Task Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Please specify the file containing the command ids");
                }

                string commandsFile = args[0];

                string iniFile = "CosmosDbConfig.ini";
                if (args.Length == 2)
                {
                    iniFile = args[1];
                }

                Console.WriteLine($"Get command ids from {commandsFile} and storage key from {iniFile}...");

                Dictionary<string,string> settings = ReadINISetting(@".\CosmosDbConfig.ini");

                PrimaryKey = settings["PrimaryKey"];
                EndpointUrl = settings["EndpointUrl"];

                // Use Azure.Identity to get secrets
                //var secretUri = "https://pcf-prod-ame.vault.azure.net";
                //var client = new SecretClient(new Uri(secretUri), new DefaultAzureCredential());

                ///PrivaryKey = client.GetSecret("prod-commandhistory-key").Value;

                var commands = File.ReadLines(commandsFile);

                Program p = new Program();
                var results = await p.GetCommandsAsync(commands.Select(x => $@"""{x.Replace("-", string.Empty)}""").ToList());
            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}\n", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}\n", e);
            }
            finally
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        // Simplistic INI file reader; simple returns a dictionary containing any lines that match Key=Value
        static Dictionary<string, string> ReadINISetting(string iniFilePath)
        {
            var lines = File.ReadAllLines(iniFilePath);
            var dict = new Dictionary<string, string>();

            foreach (var s in lines)
            {
                var split = s.Split(new char[] { '=' }, 2);
                if (split.Count() == 2)
                {
                    dict.Add(split[0], split[1]);
                }
            }

            return dict;
        }

        /// <summary>
        /// Get the PXS portion of the command info from CommandHistoryDb
        /// </summary>
        public async Task<List<string>> GetCommandsAsync(List<string> commands)
        {
            List<string> commandInfo = new List<string>();

            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUrl, PrimaryKey);
            
            this.database = this.cosmosClient.GetDatabase(databaseName);

            this.container = this.database.GetContainer(containerName);



            int numCommands = commands.Count;
            int startIndex = 0;
            int maxBatchSize = 5000;
            int batchSize;
            int count = 0;
            using (StreamWriter commandsFoundFile = new StreamWriter("commandsFound.txt", append: true))
              using (StreamWriter commandsDetailFile = new StreamWriter("commandsDetail.json", append: true))
            { 
                while (startIndex < numCommands)
                {
                    batchSize = (startIndex + maxBatchSize < numCommands ? maxBatchSize : numCommands - startIndex);

                    Console.WriteLine($"Processing commands {startIndex} to {startIndex + batchSize - 1}");
                    var currentCommands = commands.GetRange(startIndex, batchSize);

                    string commandIds = string.Join(",", currentCommands.ToArray());

                    // While testing, it's simpler to grab commands from PPE coldstorage and insert them here
                    //string commandIds = @"""8038d221ca3d4c7b9cb7763fff7ab0b9"", ""d9d1d7bad5e74949980c53b7883faacc""";

                    var iterator = this.container.GetItemQueryIterator<JObject>($"SELECT c.pxs FROM c WHERE c.id IN ({commandIds})");

                    while (iterator.HasMoreResults)
                    {
                        foreach (var item in (await iterator.ReadNextAsync()).Resource)
                        {
                            // Ideally we would convert to a PrivacyRequest object;
                            // but this is a base class, so we'd need to do something fancier to
                            // know which object to use for the deserialization
                            //var request = JsonConvert.DeserializeObject<PrivacyRequest>(json);
                            //request.VerificationToken = null;
                            //Console.WriteLine(request.ToString());
                            //commandsFoundFile.WriteLine(request.RequestId);
                            //commandsDetailFile.WriteLine(request.ToString());

                            // remove verifier token 
                            item["pxs"]["verificationToken"] = "";

                            // save the command id so we know which ones were found
                            commandsFoundFile.WriteLine(item["pxs"]["requestId"]);

                            // flatten the json output
                            var json = item.ToString().Replace("\r\n", "").Replace("\t", "").Replace("    ", "");

                            Console.WriteLine(json);

                            // Save the request details
                            commandsDetailFile.WriteLine(json);

                            // Not really using this, but leaving here in case we decide to
                            // do more processing.
                            commandInfo.Add(json);

                            count++;
                        }
                    }

                    startIndex += batchSize;
                }
            }

            Console.WriteLine($"Total commands found = {count}");
            return commandInfo;
        }
    }
}
