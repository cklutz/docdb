// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

exports.transform = function (model) {
    if (!model) return null;

    if (model.payload.type) {
        model['is' + model.payload.type] = true;
        switch (model.payload.type.toLowerCase()) {
            case 'table':
            case 'view':
                model.isTabular = true;
                break;
            case 'userdefinedfunction':
            case 'storedprocedure':
                model.isProgrammability = true;
                break;
        }
    }

    return model;
}
