using System;


namespace DocDB;

internal interface IModelInfo
{
    string? SchemaVersion { get; }
    DateTime LastSchemaModificationAt { get; }
    string DatabaseId { get; }
    string DatabaseName { get; }
}
