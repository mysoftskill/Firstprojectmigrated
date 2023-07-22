To run the tool:

.\GetPcfCommands <file containing command ids> [<config file>]


Read a list of command ids from a file and query the ComandHistory database for details.
This program currently extracts only the pxs object from the CommandHistory.  To obtain all 
details, you will need to update the SQL query in the code.

     Arguments:
          args[0] - the file containing the commands (required)
          		Format: one command id (guid) per line; guid can contain dashes or not
          args[1] - an ini file with the database endpoint url and primary key (defaults to .\CosmosDbConfig.ini
                      Format:
                      EndpointUrl=https:....
                      PrimaryKey=<encrypted key>
     Output:
     	commandsFound.txt - the list of commands that were successfully found
     	commandsDetails.json - the json output with the details from the c.pxs element

To get the Primary Key, you will need to go to the appropriate osmosDb in Azure.

If you are running this in Prod, you should get the appropriate JIT requests (see Team OneNote for instructions)
to log into a Prod machine and copy the program and run it there.
