/*
 *
 * The stored procedure in CosmosDB to delete items off of the command queue transactionally.
 * Parameters:
 *   - flushTimestamp -- the timestamp of the last command that needs to be flushed.
 *   - maxItems -- the maximum number of items to delete from the queue.
 *   - partitionKey -- the partition key.
 */

function flushAgentQueue(flushTimestamp, maxItems, partitionKey) {
    var context = getContext();
    var response = context.getResponse();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();

    var responseObject = {
        deleted: 0,
        total: -1
    };

    var query = {
        query: 'SELECT TOP @maxItems * FROM c WHERE c.ts <= @flushTimestamp AND c.pk = @partitionKey',
        parameters: [
            { name: "@maxItems", value: maxItems },
            { name: "@flushTimestamp", value: flushTimestamp },
            { name: "@partitionKey", value: partitionKey }
        ]
    };

    // Query the set of documents that are eligible for delete.
    var queryDocuments = function () {
        return collection.queryDocuments(
            collectionLink,
            query,
            { pageSize: 100 },
            function (err, documents, responseOptions) {
                if (err) throw err;

                responseObject.total = documents.length;

                for (var i = 0; i < documents.length; ++i) {
                    // Update the compound key.
                    var document = documents[i];

                    var accepted = deleteDocument(document);
                    if (!accepted) {
                        break;
                    }
                }

                response.setBody(responseObject);
            });
    };

    // Deletes a single document.
    var deleteDocument = function (doc) {
        var accepted = collection.deleteDocument(
            doc._self,
            {},
            function (err, responseOptions) {
                if (err) {
                    // Doc skipped, someone else touched it.
                }
                else {
                    responseObject.deleted++;
                }
            });

        return accepted;
    };

    queryDocuments();
    response.setBody(responseObject);
}