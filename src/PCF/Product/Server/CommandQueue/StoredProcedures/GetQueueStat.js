/*
 *
 * The stored procedure in CosmosDB to calculate queue statistics
 * Parameters:
 *   getDetailedStats -- true to query for very detailed counts
 *   curerntTimeSeconds -- the unix time in seconds used for next visible time queries.
 */

function agentQueueStatistics(getDetailedStats, currentTimeSeconds) {
    var context = getContext();
    var response = context.getResponse();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();
    var result = {
        minTimestamp: null,
        minNextVisibleTime: null,
        pendingCommandCount: null,
        unleasedCommandCount: null
    };

    // Start at one to avoid sending response before all queries have started.
    var pendingResponses = 1;

    var queryCompleted = function () {
        pendingResponses--;
        if (pendingResponses == 0) {
            response.setBody(result);
        }
    };

    var genericQuery = function (friendlyName, query, parameters, handler) {
        var accepted = collection.queryDocuments(
            collectionLink,
            {
                query: query,
                parameters: parameters
            },
            { pageSize: 100 },
            function (err, documents, responseOptions) {
                if (err) throw err;

                if (documents.length !== 0 && handler) {
                    handler(documents[0], responseOptions);
                }

                queryCompleted();
            }
        );

        if (accepted) {
            pendingResponses++;
        }
        else {
            console.log(friendlyName + " was not accepted!");
        }
    };

    var queryOldestCommands = function () {

        genericQuery(
            "GetOldestPendingCommand",
            'SELECT TOP 1 c.ts AS MinTimestamp FROM c ORDER BY c.ts ASC',
            [],
            function (doc) {
                result.minTimestamp = doc.MinTimestamp;
            });
    };


    var queryMinLeaseCommands = function () {

        genericQuery(
            "GetMinLeaseTime",
            'SELECT TOP 1 c.nvt AS MinLeaseTime FROM c ORDER BY c.nvt ASC',
            [],
            function (doc) {
                result.minNextVisibleTime = doc.MinLeaseTime;
            });
    };

    var getPendingCommandCount = function () {
        genericQuery(
            "GetPendingCommandCount",
            "SELECT COUNT(1) AS Count FROM c",
            [],
            function (doc, responseOptions) {
                result.pendingCommandCount = doc.Count;
            });
    };

    var getUnleasedCommandCount = function (callback) {
        genericQuery(
            "GetUnleasedCommandCount",
            "SELECT COUNT(1) AS Count FROM c WHERE c.nvt < @currentTimeSeconds ORDER BY c.nvt ASC",
            [
                { name: "@currentTimeSeconds", value: currentTimeSeconds }
            ],
            function (doc, responseOptions) {
                result.unleasedCommandCount = doc.Count;
            });
    };

    queryMinLeaseCommands();
    queryOldestCommands();

    if (getDetailedStats) {
        getPendingCommandCount();
        getUnleasedCommandCount();
    }

    // Complete the "default" query.
    queryCompleted();
}
