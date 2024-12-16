
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SmoIndex = Microsoft.SqlServer.Management.Smo.Index;

namespace DocDB;

public sealed class SqlServerTarget : IDisposable
{
    private bool _disposed;
    private readonly Server _server;
    private readonly ServerConnection _connection;
    private readonly Database _database;

    public SqlServerTarget(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connection = new SqlConnection(connectionString);
        try
        {
            // ServerConnection adopts SqlConnection; if creation fails, we need to dispose it manually.
            _connection = new ServerConnection(connection);
        }
        catch (Exception)
        {
            connection.Dispose();
            throw;
        }

        _server = new Server(_connection);

        _database = _server.Databases[_server.ConnectionContext.DatabaseName];

        _server.SetDefaultInitFields(typeof(Schema), nameof(Schema.IsSystemObject));
        _server.SetDefaultInitFields(typeof(Table), nameof(Table.IsSystemObject));
        _server.SetDefaultInitFields(typeof(View), nameof(View.IsSystemObject));
        _server.SetDefaultInitFields(typeof(StoredProcedure), nameof(StoredProcedure.IsSystemObject));
        _server.SetDefaultInitFields(typeof(UserDefinedFunction), nameof(UserDefinedFunction.IsSystemObject));
        _server.SetDefaultInitFields(typeof(UserDefinedAggregate), nameof(UserDefinedAggregate.AssemblyName));
        _server.SetDefaultInitFields(typeof(DatabaseDdlTrigger), nameof(DatabaseDdlTrigger.IsSystemObject));
        _server.SetDefaultInitFields(typeof(SmoIndex), nameof(SmoIndex.IsSystemObject));
        _server.SetDefaultInitFields(typeof(Trigger), nameof(Trigger.IsSystemObject));
        _server.SetDefaultInitFields(typeof(DatabaseRole), nameof(DatabaseRole.IsFixedRole));
        _server.SetDefaultInitFields(typeof(User), nameof(User.IsSystemObject));
        _server.SetDefaultInitFields(typeof(SqlAssembly), nameof(SqlAssembly.IsSystemObject));
    }

    private IEnumerable<Urn> AddUrns<T>(SmoCollectionBase source, Func<T, bool>? predicate = null) where T : SqlSmoObject
    {
        // Some objects are not supported on Azure or Standalone
        if (_database.IsSupportedObject<T>())
        {
            return predicate == null ? source.OfType<T>().Select(t => t.Urn) : source.OfType<T>().Where(predicate).Select(t => t.Urn);
        }

        return [];
    }

    private Lazy<IEnumerable<Urn>> _userObjectUrns => new(()
        =>
        new[] { _database.Urn }
        .Union(AddUrns<Table>(_database.Tables, t => !t.IsSystemObject))
        .Union(AddUrns<View>(_database.Views, t => !t.IsSystemObject))
        .Union(AddUrns<StoredProcedure>(_database.StoredProcedures, t => !t.IsSystemObject))
        .Union(AddUrns<UserDefinedFunction>(_database.UserDefinedFunctions, t => !t.IsSystemObject))
        .Union(AddUrns<UserDefinedAggregate>(_database.UserDefinedAggregates))
        .Union(AddUrns<UserDefinedTableType>(_database.UserDefinedTableTypes))
        .Union(AddUrns<UserDefinedDataType>(_database.UserDefinedDataTypes))
        .Union(AddUrns<UserDefinedType>(_database.UserDefinedTypes))
        .Union(AddUrns<SqlAssembly>(_database.Assemblies))
        .Union(AddUrns<DatabaseDdlTrigger>(_database.Triggers, t => !t.IsSystemObject))
        .Union(AddUrns<XmlSchemaCollection>(_database.XmlSchemaCollections))
        //.Union(AddUrns<PartitionFunction>(_database.PartitionFunctions))
        //.Union(AddUrns<PartitionScheme>(_database.PartitionSchemes))
        .Union(AddUrns<Sequence>(_database.Sequences, r => true))
        .Union(AddUrns<Rule>(_database.Rules, r => true))
        .Union(AddUrns<Default>(_database.Defaults, r => true))
        .Union(AddUrns<DatabaseRole>(_database.Roles, r => true))
        .Union(AddUrns<ApplicationRole>(_database.ApplicationRoles))
        .Union(AddUrns<User>(_database.Users, t => t.IsSystemObject))
        .Union(AddUrns<Schema>(_database.Schemas, t => (!t.IsSystemSchema() && t.EnumOwnedObjects().Length > 0) || t.Name == "dbo"))
        , LazyThreadSafetyMode.ExecutionAndPublication);

    public IEnumerable<Urn> Objects => _userObjectUrns.Value;

    public NamedSmoObject GetSmoObject(Urn urn)
    {
        ArgumentNullException.ThrowIfNull(urn);
        return (NamedSmoObject)_server.GetSmoObject(urn);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Disconnect();
            _disposed = true;
        }
    }
}
