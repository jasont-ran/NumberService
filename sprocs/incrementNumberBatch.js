function incrementNumberBatch(key, clientId, batchSize) {
    const asyncHelper = {
        readDocument(key) {
            return new Promise((resolve, reject) => {
                var docLink = __.getAltLink() + "/docs/" + key;

                const isAccepted = __.readDocument(docLink, {}, (err, resource, options) => {
                    if (err) resolve(null);
                    resolve(resource);
                });

                if (!isAccepted) reject(new Error(ERROR_CODE.NotAccepted, "readDocument was not accepted."));
            });
        },

        createDocument(key, number, clientId) {
            return new Promise((resolve, reject) => {
                var docLink = __.getAltLink() + "/docs/" + key;

                const isAccepted = __.createDocument(
                    __.getSelfLink(),
                    { id: key, scopeName: key, number: number, clientId: clientId },
                    {},
                    (err, resource, options) => {
                        if (err) reject(err);
                        resolve(resource);
                    });

                if (!isAccepted) reject(new Error(ERROR_CODE.NotAccepted, "createDocument was not accepted."));
            });
        },

        replaceDocument(doc) {
            return new Promise((resolve, reject) => {
                const isAccepted = __.replaceDocument(
                    doc._self, 
                    doc, 
                    { etag: doc._etag }, 
                    (err, result, options) => {
                        if (err) reject(err);
                        resolve(result);
                    });
                if (!isAccepted) reject(new Error(ERROR_CODE.NotAccepted, "replaceDocument was not accepted."));
            });
        }
    };

    async function main(key, clientId, batchSize) {
        let doc = await asyncHelper.readDocument(key);

        if (!doc) {
            console.log("doc not found, creating doc with key = ", key);
            doc = await asyncHelper.createDocument(key, 0, clientId);
        }
        let startNumber = doc.number + 1;
        let endNumber = doc.number + batchSize;
        doc.number = endNumber;
        doc.start
        doc.clientId = clientId;

        var newDoc = await asyncHelper.replaceDocument(doc);
        
        newDoc.startNumber = startNumber;
        newDoc.endNumber = endNumber;

        getContext().getResponse().setBody(newDoc);
    }

    main(key, clientId, batchSize).catch(err => getContext().abort(err));
}
