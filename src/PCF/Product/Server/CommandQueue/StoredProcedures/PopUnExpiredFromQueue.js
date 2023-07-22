/*
 *
 * The stored procedure in CosmosDB to pop items off of the command queue transactionally.
 * Parameters:
 *   - minTimespan -- min creation time for the records. Anything older is considered expired.
 *   - minCk -- the minimum compound key to search. This will be {agentId}.{assetGroupId}.000000000000.
 *   - maxCk -- the compound key to search up to. This will be {agentId}.{assetGroupId}.{utc timestamp seconds, with leading zero padding}
 *   - updateCk -- the CK with timestamp at which the queue items will next be visible. This will be {agentId}.{assetGroupId}.{future next visible time}
 *   - updateNvt -- the value that 'nvt' should be updated to.
 *   - maxItems -- the maximum number of items to pop from the queue.
 *   - partitionKey -- the partition key.
 */

function popFromQueue(minTimespan, minCk, maxCk, updateCk, updateNvt, maxItems, partitionKey) {
    var context = getContext();
    var response = context.getResponse();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();

    var pendingReplacements = 0;

    var responseObject = {
        items: []
    };

    var getCurrentTimestamp = function () {
        var date = new Date();

        // date.getTime() returns UTC milliseconds. We want UTC seconds.
        var timestamp = Math.floor(date.getTime() / 1000);
        return timestamp;
    };
    
    var query = {
        query: 'SELECT TOP @maxItems * FROM c WHERE  (c.ts > @minTimespan) AND (c.ck BETWEEN @minCk AND @maxCk) AND c.pk = @partitionKey ORDER BY c.ck ASC',
        parameters: [
            { name: "@minTimespan", value: minTimespan },
            { name: "@maxItems", value: maxItems },
            { name: "@minCk", value: minCk },
            { name: "@maxCk", value: maxCk },
            { name: "@partitionKey", value: partitionKey }
        ]
    };

    // Query the set of documents that are eligible for dequeue.
    var queryDocuments = function () {
        return collection.queryDocuments(
              collectionLink,
              query,
              { pageSize: 100 },
              function (err, documents, responseOptions) {
                  if (err) throw err;
                  
                  for (var i = 0; i < documents.length; ++i) {
                      // Update the compound key.
                      var document = documents[i];
                      document.nvt = updateNvt;
                      document.ck = updateCk;

                      // If "ttl" is undefined or null, then set to zero.
                      if (!document.ttl) {
                          document.ttl = 0;
                      }

                      // DocDB does expiration based on last modified + TTL.
                      // So, continued updates without reducing TTL continually extends the 
                      // lifetime of a document. We do math here to lower TTL when we do an update.
                      var absoluteExpiration = document.ttl + document._ts;
                      var secondsUntilExpiration = absoluteExpiration - getCurrentTimestamp();
                      document.ttl = Math.max(secondsUntilExpiration, 1);
                      
                      var accepted = replaceDocument(document);
                      if (!accepted) {
                          break;
                      }
                  }

                  // Maybe there was no work to do. If so, then return.
                  if (pendingReplacements === 0) {
                      response.setBody(responseObject);
                  }
              });
    };

    // Replaces a single document.
    var replaceDocument = function (doc) {
        var accepted = collection.replaceDocument(
            doc._self,
            doc,
            { etag: doc._etag },
            function (err, docReplaced) {
                pendingReplacements--;

                docReplaced._ts = getCurrentTimestamp();

                if (err) {
                    // Doc skipped, someone else touched it.
                }
                else {
                    responseObject.items.push(docReplaced);
                }

                // Are we the last work item to succeed?
                if (pendingReplacements === 0) {
                    response.setBody(responseObject);
                }
            });

        if (accepted) {
            pendingReplacements++;
        }

        return accepted;
    };

    if (!queryDocuments()) {
        response.setBody(responseObject);
    }
}