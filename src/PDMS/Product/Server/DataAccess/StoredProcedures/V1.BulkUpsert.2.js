/// <reference group="Generic" />
/// <reference path="Intellisense.js" />
/**
 * A DocumentDB stored procedure that bulk upserts a list of entities.
 * If an entity contains a non-empty _etag, then the code will attempt to replace.
 * Otherwise, it will attempt to perform a create.
 * If any entity fails to be upserted, than none of the entities will be upserted.
 * Note: If the number of entities provided cannot be completed within the execution timeout window,
 * than this script will need to be updated to support continuation behavior.
 * This is not recommended because the transaction does not persist across continuations.
 *
 * @function
 * @param {string} databaseName - The name of the database. This is necessary to construct document links.
 * @param {string} collectionName - The name of the collection. This is necessary to construct document links.
 * @param {object array} entities - An array of entities that should be upserted within a single transaction.
 * @param {object array} deletions - An array of entities that should be removed within a single transation.
 * @return {object array} - The updated entity values.
 */
function bulkUpsert(databaseName, collectionName, entities, deletions) {
    var context = getContext();
    var collection = context.getCollection();

    deleteAll(); // Start with deletes. This will trigger upserts at the end.

    function deleteAll() {
        var index = 0;

        if (deletions.length > 0) {
            deleteEntity(deletions[index]);
        }
        else {
            // There are no delete items, so move onto the upserts.
            upsertAll();
        }

        function deleteEntity(entity) {
            // Use the etag to perform optimistic concurrency.
            var deleteOptions = { etag: entity._etag };

            var documentLink = createDocumentLink(entity);

            var accept = collection.deleteDocument(documentLink, deleteOptions, operationCallback);

            if (!accept) throw new Error("The server rejected the delete. Id:" + entity.id);
        }

        function operationCallback(err, document) {
            if (err) throw err;

            // Increment the index.
            index += 1;

            if (index < deletions.length) {
                // If there are more items, recursively delete them.
                deleteEntity(deletions[index]);
            }
            else {
                // There are no more items, so move onto the upserts.
                upsertAll();
            }
        }
    }

    function upsertAll() {
        var index = 0;

        if (entities.length > 0) {
            // The calls are asynchronous, so we must call them using recursion.
            upsertEntity(entities[index]);
        }

        function upsertEntity(entity) {
            // If the etag is set, then do a replace.
            if (Boolean(entity._etag)) {
                replaceEntity(entity);
            }
                // Otherwise, do a create.
            else {
                createEntity(entity);
            }
        }

        function createEntity(entity) {
            var collectionLink = createCollectionLink();

            var accept = collection.createDocument(collectionLink, entity, operationCallback);

            if (!accept) throw new Error("The server rejected the creation. Id:" + entity.id);
        }

        function replaceEntity(entity) {
            // Use the etag to perform optimistic concurrency.
            var replaceOptions = { etag: entity._etag };

            var documentLink = createDocumentLink(entity);

            var accept = collection.replaceDocument(documentLink, entity, replaceOptions, operationCallback);

            if (!accept) throw new Error("The server rejected the replacement. Id:" + entity.id);
        }

        function operationCallback(err, document) {
            if (err) throw err;

            // Store the replaced document so that we can return it in the response.
            entities[index] = document;

            // Increment the index.
            index += 1;

            if (index < entities.length) {
                // If there are more items, recursively upsert them.
                upsertEntity(entities[index]);
            }
            else {
                // There are no more items, so set the response.
                context.getResponse().setBody(entities);
            }
        }
    }

    function createCollectionLink() {
        return "dbs/" + databaseName + "/colls/" + collectionName;
    }

    function createDocumentLink(entity) {
        return createCollectionLink() + "/docs/" + entity.id;
    }
}