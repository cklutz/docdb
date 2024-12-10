var common = require('./DocDB.common.js');
var extension = require('./DocDB.extension.js');
var overwrite = require('./DocDB.overwrite.js');

exports.transform = function (model) {

    console.log("1>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> ");
    console.log(JSON.stringify(model));
    console.log("2>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> ");

    model.yamlmime = "DocDB";

    if (overwrite && overwrite.transform) {
        return overwrite.transform(model);
    }

    if (extension && extension.preTransform) {
        model = extension.preTransform(model);
    }

    if (common && common.transform) {
        model = common.transform(model);
    }

    if (extension && extension.postTransform) {
        model = extension.postTransform(model);
    }

    return model;
}