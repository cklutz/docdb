// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

function lastIndexOfAny(str, chars) {
    let lastIndex = -1;

    for (const char of chars) {
        const index = str.lastIndexOf(char);
        if (index > lastIndex) {
            lastIndex = index;
        }
    }

    return lastIndex;
}

function getDirectoryName(path) {
    if (!path) return '';
    var index = lastIndexOfAny(path, ['/', '\\']);
    return path.slice(0, index + 1);
}

function getFileName(path) {
    if (!path) return '';
    var index = lastIndexOfAny(path, ['/', '\\']);
    return path.slice(index + 1);
}

function getDisplayMaxSize(maxSize) {
    if (maxSize) {
        return `Limited to ${maxSize / 1024.0} MB`;
    }
    return 'Unlimited';
}

function getDisplayGrowth(growth) {
    if (growth) {
        return growth / 1024.0;
    }
    return growth;
}

function getGrowthDescription(growthType, growth, maxSize) {
    if (growthType && growth) {
        switch (growthType.toLowerCase()) {
            case 'kilobyte':
                return `By ${getDisplayGrowth(growth)} MB, ${getDisplayMaxSize(maxSize)}`
            case 'percent':
                return `By ${growth}%, ${getDisplayMaxSize(maxSize)}`
        }
        return 'None';
    }
    return null;
}

exports.transform = function (model) {
    if (!model) return null;

    if (model.payload.type) {
        model['is' + model.payload.type] = true;
        switch (model.payload.type.toLowerCase()) {
            case 'table':
            case 'view':
            case 'userdefinedtabletype':
                model.isTabular = true;
                break;
            case 'userdefinedfunction':
            case 'storedprocedure':
                model.isProcOrFunction = true;
                break;
            case 'database':
                model.payload.files = [...(model.payload.dataFiles || []), ...(model.payload.logFiles || [])];
                model.payload.files = model.payload.files.map(file => {
                    if (file.type === 'DataFile') {
                        file.isData = true;
                    } else {
                        file.isData = false;
                        file.fileGroup = null;
                    }
                    if (file.size) {
                        file.sizeMB = file.size / 1024.0;
                    }
                    if (file.fileName) {
                        file.path = getDirectoryName(file.fileName);
                        file.fileName = getFileName(file.fileName);
                    }
                    file.growthDescription = getGrowthDescription(file.growthType, file.growth, file.maxSize);
                    return file;
                });

                if (model.payload.fileGroups) {
                    model.payload.fileGroups = model.payload.fileGroups.map(fg => {
                        if (fg.files) {
                            fg.filesCount = fg.files.length;
                        } else {
                            fg.filesCount = 0;
                        }
                        return fg;
                    });
                }

                if (model.payload.options) {
                    model.payload.options = model.payload.options.map(category => {
                        category.entries = category.entries.map(entry => {
                            if (entry.type === "bool") {
                                if (entry.value === 'true') {
                                    return { ...entry, isBool: true, isTrue: true };
                                } else {
                                    return { ...entry, isBool: true, isTrue: false };
                                }
                            } else if (entry.type === "number") {
                                return { ...entry, isNumber: true };
                            } else if (entry.type === "enum") {
                                return { ...entry, isEnum: true };
                            } else if (entry.type === "string") {
                                return { ...entry, isString: true };
                            } else if (entry.type === "datetime") {
                                return { ...entry, isDateTime: true };
                            } else if (entry.type === "guid") {
                                return { ...entry, isGuid: true };
                            }
                            return entry;
                        });
                        return category;
                    });
                }
                break;
        }
    }

    return model;
}
