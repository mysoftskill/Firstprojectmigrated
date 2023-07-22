# AAD Account Close Dead Letter Restorer

This tool takes a configuration file as an input, and attempts to restore any object id + tenant id pairs from dead-letter storage.


The configuration file is: *input.txt*

The format of the text file should be in the form: [object-id],[tenant-id]

*Example*: 8c901c72-205b-488b-9d37-61a86c448b89,b8f9a024-228b-4f5f-808c-40be305cda89<BR>

Repeat for every object id + tenant id pair.

The flow works as follows:
1. App reads configuration file to see if anything needs restored
2. For each object id + tenant id, read from dead-letter storage
3. Enqueue to Azure queue (this lets the worker role pick them up again)
4. Repeat for each value found in configuration file
5. Exit app when completed

*NOTE: This does NOT delete anything from dead-letter storage. It's permanently there. This tool could be expanded to take care of that, if needed. But there is no harm in keeping things there.*