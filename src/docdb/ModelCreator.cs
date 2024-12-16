using DocDB.Contracts;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SmoIndex = Microsoft.SqlServer.Management.Smo.Index;


namespace DocDB;

internal class ModelCreator
{
    private readonly string _databaseId;

    public ModelCreator(Database database)
    {
        _databaseId = database.GetModelId();
    }

    public DdbObject? CreateObject(NamedSmoObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj switch
        {
            Database database => CreateDatabase(database),
            Table table => CreateTable(table),
            View view => CreateView(view),
            Schema schema => CreateSchema(schema),
            User user => CreateUser(user),
            DatabaseRole databaseRole => CreateDatabaseRole(databaseRole),
            DatabaseDdlTrigger ddlTrigger => CreateDatabaseDdlTrigger(ddlTrigger),
            ApplicationRole applicationRole => CreateApplicationRole(applicationRole),
            UserDefinedFunction udf => CreateUserDefinedFunction(udf),
            UserDefinedAggregate uda => CreateUserDefinedAggregate(uda),
            UserDefinedDataType udt => CreateUserDefinedDataType(udt),
            UserDefinedTableType udt => CreateUserDefinedTableType(udt),
            StoredProcedure sp => CreateStoredProcedure(sp),
            SqlAssembly assembly => CreateAssembly(assembly),
            UserDefinedType udt => CreateUserDefinedType(udt),
            Rule rule => CreateRule(rule),
            Default def => CreateDefault(def),
            Sequence sequence => CreateSequence(sequence),
            XmlSchemaCollection collection => CreateXmlSchemaCollection(collection),
            _ => throw new ArgumentOutOfRangeException(nameof(obj), obj.GetType(), null)
        };
    }

    private T InitBase<T>(T obj, NamedSmoObject smo, bool noScript = false) where T : DdbObject
    {
        // TODO: Via reflection (no common base class?) set the CreatedAt, LastModifiedAt members

        obj.DatabaseId = _databaseId;
        obj.Id = smo.GetModelId();

        if (obj is NamedDdbObject named)
        {
            named.Name = smo.GetFullName(quote: false);
        }

        if (smo is IExtendedProperties extendedProperties)
        {
            obj.Description = extendedProperties.GetMSDescription();
        }

        if (smo is IScriptable && !noScript)
        {
            var scriptingOptions = new ScriptingOptions();
            if (obj is DdbTable || obj is DdbView)
            {
                scriptingOptions.Indexes = true;
                scriptingOptions.Triggers = true;
                scriptingOptions.DriAll = true;
            }
            obj.Script = smo.GetScript(scriptingOptions);
        }

        return obj;
    }

    private DdbDatabase CreateDatabase(Database database)
    {
        var result = InitBase(new DdbDatabase(), database);

        // These are (hopefully) vendor/product agnostic.
        result.Collation = database.Collation;
        result.IsCloudHosted = database.DatabaseEngineType == Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase;
        result.ProductInformation = "Microsoft " + database.GetSqlServerVersionName();
        result.ProductType = DdbServerProductType.SqlServer;

        foreach (FileGroup fileGroup in database.FileGroups)
        {
            var fg = CreateFileGroup(fileGroup);

            foreach (DataFile dataFile in fileGroup.Files)
            {
                var file = CreateDataFile(dataFile);
                file.FileGroup = fg.ToNamedRef();
                fg.Files.Add(file.ToNamedRef());
                result.DataFiles.Add(file);
            }

            result.FileGroups.Add(fg);
        }

        foreach (LogFile lf in database.LogFiles)
        {
            result.LogFiles.Add(CreateLogFile(lf));
        }


        // Engine Options
        var engine = new DdbOptionCategory("engine");
        // Explicitly invoke properties; accessing them via "database.Properties" is not supported. Go figure.
        SetOption(database, engine, nameof(Database.DatabaseEngineType), d => d.DatabaseEngineType);
        SetOption(database, engine, nameof(Database.DatabaseEngineEdition), d => d.DatabaseEngineEdition);
        SetOption(database, engine, "ServerVersion", d => d.ServerVersion);
        SetOption(database, engine, "ServerVersionName", d => "Microsoft " + d.GetSqlServerVersionName());
        result.Options.Add(engine);

        // These are the options (mostly) that are exposed via SSMS' database properties dialog.
        // Note that they are not directly exposed as properties on the DdbDatabase class. We
        // do this on purpose so that we can also support other database engines, that may have
        // different options.
        // Also note that there are quite some more Database properties. Might include them here
        // in the future, if required.

        // Automatic Options
        var automatic = new DdbOptionCategory("automatic");
        SetOption(database, automatic, nameof(Database.AutoClose));
        SetOption(database, automatic, nameof(Database.AutoCreateIncrementalStatisticsEnabled));
        SetOption(database, automatic, nameof(Database.AutoCreateStatisticsEnabled));
        SetOption(database, automatic, nameof(Database.AutoUpdateStatisticsAsync));
        SetOption(database, automatic, nameof(Database.AutoUpdateStatisticsEnabled));
        SetOption(database, automatic, nameof(Database.AutoShrink));
        result.Options.Add(automatic);

        // Containment Options
        var containment = new DdbOptionCategory("containment");
        SetOption(database, containment, nameof(Database.ContainmentType));
        SetOption(database, containment, nameof(Database.DefaultFullTextLanguage), getter: d => d.DefaultFullTextLanguage.Lcid);
        SetOption(database, containment, nameof(Database.DefaultLanguage), getter: d => d.DefaultLanguage.Name);
        SetOption(database, containment, nameof(Database.NestedTriggersEnabled));
        SetOption(database, containment, nameof(Database.TransformNoiseWords));
        SetOption(database, containment, nameof(Database.TwoDigitYearCutoff));
        result.Options.Add(containment);

        // Cursor Options
        var cursor = new DdbOptionCategory("cursor");
        SetOption(database, cursor, nameof(Database.CloseCursorsOnCommitEnabled));
        SetOption(database, cursor, nameof(Database.LocalCursorsDefault));
        result.Options.Add(cursor);

        // Cursor Options
        var databaseScopedConfigurations = new DdbOptionCategory("databaseScopedConfigurations");
        SetOption(database, databaseScopedConfigurations, nameof(Database.LegacyCardinalityEstimation));
        SetOption(database, databaseScopedConfigurations, nameof(Database.LegacyCardinalityEstimationForSecondary));
        SetOption(database, databaseScopedConfigurations, nameof(Database.MaxDop));
        SetOption(database, databaseScopedConfigurations, nameof(Database.MaxDopForSecondary));
        SetOption(database, databaseScopedConfigurations, nameof(Database.ParameterSniffing));
        SetOption(database, databaseScopedConfigurations, nameof(Database.ParameterSniffingForSecondary));
        SetOption(database, databaseScopedConfigurations, nameof(Database.QueryOptimizerHotfixes));
        SetOption(database, databaseScopedConfigurations, nameof(Database.QueryOptimizerHotfixesForSecondary));
        result.Options.Add(databaseScopedConfigurations);

        // FILESTREAM Options
        var fileStream = new DdbOptionCategory("fileStream");
        SetOption(database, fileStream, nameof(Database.FilestreamDirectoryName));
        SetOption(database, fileStream, nameof(Database.FilestreamNonTransactedAccess));
        result.Options.Add(fileStream);

        // Ledger Options
        var ledger = new DdbOptionCategory("ledger");
        SetOption(database, ledger, nameof(Database.IsLedger));
        result.Options.Add(ledger);

        // Miscellaneous Options
        var miscellaneous = new DdbOptionCategory("miscellaneous");
        SetOption(database, miscellaneous, nameof(Database.CompatibilityLevel));
        SetOption(database, miscellaneous, nameof(Database.SnapshotIsolationState));
        SetOption(database, miscellaneous, nameof(Database.AnsiNullDefault));
        SetOption(database, miscellaneous, nameof(Database.AnsiNullsEnabled));
        SetOption(database, miscellaneous, nameof(Database.AnsiPaddingEnabled));
        SetOption(database, miscellaneous, nameof(Database.AnsiWarningsEnabled));
        SetOption(database, miscellaneous, nameof(Database.ArithmeticAbortEnabled));
        SetOption(database, miscellaneous, nameof(Database.ConcatenateNullYieldsNull));
        SetOption(database, miscellaneous, nameof(Database.DatabaseOwnershipChaining));
        SetOption(database, miscellaneous, nameof(Database.DateCorrelationOptimization));
        SetOption(database, miscellaneous, nameof(Database.DelayedDurability));
        SetOption(database, miscellaneous, nameof(Database.IsReadCommittedSnapshotOn));
        SetOption(database, miscellaneous, nameof(Database.NumericRoundAbortEnabled));
        SetOption(database, miscellaneous, nameof(Database.IsParameterizationForced));
        SetOption(database, miscellaneous, nameof(Database.QuotedIdentifiersEnabled));
        SetOption(database, miscellaneous, nameof(Database.RecursiveTriggersEnabled));
        SetOption(database, miscellaneous, nameof(Database.Trustworthy));
        SetOption(database, miscellaneous, nameof(Database.IsVarDecimalStorageFormatEnabled));
        SetOption(database, miscellaneous, nameof(Database.IsVarDecimalStorageFormatSupported));
        result.Options.Add(miscellaneous);

        // Recovery Options
        var recovery = new DdbOptionCategory("recovery");
        SetOption(database, recovery, nameof(Database.RecoveryModel));
        SetOption(database, recovery, nameof(Database.PageVerify));
        SetOption(database, recovery, nameof(Database.TargetRecoveryTime));
        result.Options.Add(recovery);

        // Service Broker Options
        var serviceBroker = new DdbOptionCategory("serviceBroker");
        SetOption(database, serviceBroker, nameof(Database.BrokerEnabled));
        SetOption(database, serviceBroker, nameof(Database.HonorBrokerPriority));
        SetOption(database, serviceBroker, nameof(Database.ServiceBrokerGuid));
        result.Options.Add(serviceBroker);

        // State Options
        var state = new DdbOptionCategory("state");
        SetOption(database, state, nameof(Database.ReadOnly));
        SetOption(database, state, nameof(Database.Status));
        SetOption(database, state, nameof(Database.EncryptionEnabled));
        result.Options.Add(state);

        // Query Store Options
        var queryStore = new DdbOptionCategory("queryStore");
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.ActualState));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.DataFlushIntervalInSeconds));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.StatisticsCollectionIntervalInMinutes));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.MaxPlansPerQuery));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.MaxStorageSizeInMB));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.QueryCaptureMode));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.SizeBasedCleanupMode));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.StaleQueryThresholdInDays));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.WaitStatsCaptureMode));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.CapturePolicyExecutionCount));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.CapturePolicyStaleThresholdInHrs));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS));
        SetOption(() => database.QueryStoreOptions, queryStore, nameof(QueryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS));
        result.Options.Add(queryStore);

        // Change Tracking Options
        var changeTracking = new DdbOptionCategory("changeTracking");
        SetOption(database, changeTracking, nameof(Database.ChangeTrackingEnabled));
        SetOption(database, changeTracking, nameof(Database.ChangeTrackingRetentionPeriod));
        SetOption(database, changeTracking, nameof(Database.ChangeTrackingRetentionPeriodUnits));
        SetOption(database, changeTracking, nameof(Database.ChangeTrackingAutoCleanUp));
        result.Options.Add(changeTracking);

        // Mirroring Options
        var mirroring = new DdbOptionCategory("mirroring");
        SetOption(database, mirroring, nameof(Database.IsMirroringEnabled));
        SetOption(database, mirroring, nameof(Database.MirroringID));
        SetOption(database, mirroring, nameof(Database.MirroringPartner));
        SetOption(database, mirroring, nameof(Database.MirroringPartnerInstance));
        SetOption(database, mirroring, nameof(Database.MirroringRedoQueueMaxSize));
        SetOption(database, mirroring, nameof(Database.MirroringSafetyLevel));
        SetOption(database, mirroring, nameof(Database.MirroringWitness));
        result.Options.Add(mirroring);

        return result;
    }

    private DdbFileGroup CreateFileGroup(FileGroup fg)
    {
        var result = InitBase(new DdbFileGroup()
        {
            IsDefault = fg.IsDefault,
            FileGroupType = fg.FileGroupType switch
            {
                FileGroupType.RowsFileGroup => "Rows",
                FileGroupType.FileStreamDataFileGroup => "FileStream",
                FileGroupType.MemoryOptimizedDataFileGroup => "MemoryOptimized",
                _ => null
            }
        }, fg, noScript: true);
        return result;
    }

    private DdbDataFile CreateDataFile(DataFile file)
    {
        var result = InitBase(new DdbDataFile()
        {
            FileName = file.FileName,
            Growth = file.Growth,
            GrowthType = file.GrowthType switch
            {
                FileGrowthType.KB => DdbGrowthType.KiloByte,
                FileGrowthType.Percent => DdbGrowthType.Percent,
                FileGrowthType.None => DdbGrowthType.Fixed,
                _ => DdbGrowthType.Unknown
            },
            IsOffline = file.IsOffline,
            IsReadOnly = file.IsReadOnly,
            IsReadOnlyMedia = file.IsReadOnlyMedia,
            IsSparse = file.IsSparse,
            Size = file.Size,
            MaxSize = file.MaxSize == -1 || file.MaxSize == 0 ? null : file.MaxSize,
        }, file, noScript: true);
        return result;
    }

    private DdbLogFile CreateLogFile(LogFile file)
    {
        var result = InitBase(new DdbLogFile()
        {
            FileName = file.FileName,
            Growth = file.Growth,
            GrowthType = file.GrowthType switch
            {
                FileGrowthType.KB => DdbGrowthType.KiloByte,
                FileGrowthType.Percent => DdbGrowthType.Percent,
                FileGrowthType.None => DdbGrowthType.Fixed,
                _ => DdbGrowthType.Unknown
            },
            IsOffline = file.IsOffline,
            IsReadOnly = file.IsReadOnly,
            IsReadOnlyMedia = file.IsReadOnlyMedia,
            IsSparse = file.IsSparse,
            Size = file.Size,
            MaxSize = file.MaxSize == -1 || file.MaxSize == 0 ? null : file.MaxSize,
        }, file, noScript: true);
        return result;
    }

    private DdbSchema CreateSchema(Schema schema)
    {
        var result = InitBase(new DdbSchema
        {
            Owner = schema.Owner,
        }, schema);
        return result;
    }

    private DdbUser CreateUser(User user)
    {
        // TODO: "OwningSchemas" (see comment in CreateDatabaseRole(), which also applies here)

        var result = InitBase(new DdbUser
        {
            AuthenticationType = user.AuthenticationType.ToString(),
            HasDBAccess = user.HasDBAccess,
            LoginType = user.LoginType.ToString(),
            Login = user.Login,
            UserType = user.UserType.ToString()
        }, user);
        return result;
    }

    private DdbDatabaseRole CreateDatabaseRole(DatabaseRole role)
    {
        // TODO: "OwningSchemas" would need to be resolved by comparing the role's name with
        // the schemas "Owner" property. However, that is not sufficient. There might by user
        // by the same name as the role and that might be the owner instead.
        // We would need the "Principal ID" (which should be unique between users and roles)
        // and check for that.

        var result = InitBase(new DdbDatabaseRole
        {
            Owner = role.Owner,
            Members = role.EnumMembers().OfType<string>().ToList()
        }, role);
        return result;
    }

    private DdbApplicationRole CreateApplicationRole(ApplicationRole role)
    {
        var result = InitBase(new DdbApplicationRole
        {
            DefaultSchema = role.DefaultSchema,
        }, role);
        return result;
    }

    private DdbDatabaseDdlTrigger CreateDatabaseDdlTrigger(DatabaseDdlTrigger trigger)
    {
        var result = InitBase(new DdbDatabaseDdlTrigger
        {
            AnsiNulls = trigger.AnsiNullsStatus,
            QuotedIdentifier = trigger.QuotedIdentifierStatus,
            ImplementationType = trigger.ImplementationType.ToString(),
            IsEnabled = trigger.IsEnabled,
            IsEncrypted = trigger.IsEncrypted,
            NotForReplication = trigger.NotForReplication,
            Text = trigger.Text,
            AssemblyName = trigger.AssemblyName,
            AssemblyRef = trigger.Parent.Assemblies.FindFirstOrDefault<SqlAssembly>(trigger.AssemblyName)?.ToNamedRef<DdbAssembly>(),
            ClassName = trigger.ClassName,
            MethodName = trigger.MethodName
        }, trigger);

        return result;
    }

    private DdbUserDefinedAggregate CreateUserDefinedAggregate(UserDefinedAggregate uda)
    {
        var result = InitBase(new DdbUserDefinedAggregate
        {
            AssemblyName = uda.AssemblyName,
            AssemblyRef = uda.Parent.Assemblies.FindFirstOrDefault<SqlAssembly>(uda.AssemblyName)?.ToNamedRef<DdbAssembly>(),
            ClassName = uda.ClassName,
            IsSchemaOwned = uda.IsSchemaOwned,
            Owner = uda.Owner,
            ReturnDataType = uda.DataType.PrettyName(),
            ReturnDataTypeRef = ResolveDataType(uda.Parent, uda.DataType),
            Syntax = uda.GetSyntax()
        }, uda);

        foreach (UserDefinedAggregateParameter parameter in uda.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbUserDefinedAggregateParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                DataTypeRef = ResolveDataType(uda.Parent, parameter.DataType),
                Description = parameter.GetMSDescription(),
                DefaultValue = null,
            }, parameter));
        }

        return result;
    }

    private DdbUserDefinedFunction CreateUserDefinedFunction(UserDefinedFunction udf)
    {
        var result = InitBase(new DdbUserDefinedFunction
        {
            CreatedAt = udf.CreateDate,
            LastModifiedAt = udf.DateLastModified,
            ReturnsNullOnNullInput = udf.ReturnsNullOnNullInput,
            FunctionType = udf.FunctionType switch
            {
                UserDefinedFunctionType.Scalar => DdbUserDefinedFunctionType.Scalar,
                UserDefinedFunctionType.Table => DdbUserDefinedFunctionType.Table,
                UserDefinedFunctionType.Inline => DdbUserDefinedFunctionType.Inline,
                _ => DdbUserDefinedFunctionType.Unknown,
            },
            Syntax = udf.GetSyntax()
        }, udf);

        foreach (UserDefinedFunctionParameter parameter in udf.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbUserDefinedFunctionParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                DataTypeRef = ResolveDataType(udf.Parent, parameter.DataType),
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            }, parameter));
        }

        return result;
    }

    private DdbStoredProcedure CreateStoredProcedure(StoredProcedure sp)
    {
        var result = InitBase(new DdbStoredProcedure
        {
            CreatedAt = sp.CreateDate,
            LastModifiedAt = sp.DateLastModified,
            Syntax = sp.GetSyntax()
        }, sp);


        foreach (StoredProcedureParameter parameter in sp.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbStoredProcedureParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                DataTypeRef = ResolveDataType(sp.Parent, parameter.DataType),
                IsOutputParameter = parameter.IsOutputParameter,
                IsCursorParameter = parameter.IsCursorParameter,
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            }, parameter));
        }

        return result;
    }

    private DdbAssembly CreateAssembly(SqlAssembly assembly)
    {
        string? assemblyName = null;
        try
        {
            var an = new AssemblyName(assembly.Name)
            {
                CultureName = assembly.Culture,
                Version = assembly.Version
            };
            an.SetPublicKeyToken(assembly.PublicKey);
            assemblyName = an.ToString();
        }
        catch (Exception)
        {
            // We cannot trust the individual values that make up the assembly name.
        }

        // TODO: Use ILSpy "lib" to decompile assembly? ;-)

        var result = InitBase(new DdbAssembly
        {
            AssemblyName = assemblyName,
            AssemblySecurityLevel = assembly.AssemblySecurityLevel.ToString(),
            Culture = assembly.Culture,
            IsVisible = assembly.IsVisible,
            Owner = assembly.Owner,
            PublicKey = assembly.PublicKey.ToHexString(),
            Version = assembly.Version.ToString(4),
            FileNames = assembly.SqlAssemblyFiles.OfType<SqlAssemblyFile>().Select(f => f.Name).ToList()
        }, assembly);

        return result;
    }

    private DdbUserDefinedType CreateUserDefinedType(UserDefinedType type)
    {
        var result = InitBase(new DdbUserDefinedType
        {
            AssemblyName = type.AssemblyName,
            AssemblyRef = type.Parent.Assemblies.FindFirstOrDefault<SqlAssembly>(type.AssemblyName)?.ToNamedRef<DdbAssembly>(),
            ClassName = type.ClassName,
            Collation = type.Collation,
            UserDefinedTypeFormat = type.UserDefinedTypeFormat.ToString(),
            BinaryTypeIdentifier = type.BinaryTypeIdentifier.ToHexString(),
            IsComVisible = type.IsComVisible,
            IsBinaryOrdered = type.IsBinaryOrdered,
            IsFixedLength = type.IsFixedLength,
            IsSchemaOwned = type.IsSchemaOwned,
            MaxLength = type.MaxLength == 0 || type.MaxLength == -1 ? null : type.MaxLength,
            IsNullable = type.IsNullable,
            NumericPrecision = type.NumericPrecision,
            NumericScale = type.NumericScale,
            Owner = type.Owner,
        }, type);

        return result;
    }

    private DdbXmlSchemaCollection CreateXmlSchemaCollection(XmlSchemaCollection collection)
    {
        var result = InitBase(new DdbXmlSchemaCollection(), collection);

        try
        {
            int i = 0;
            var doc = XDocument.Parse("<__root__>" + collection.Text + "</__root__>");
            foreach (var schema in doc.Root!.Elements(XName.Get("{http://www.w3.org/2001/XMLSchema}schema")))
            {
                string? targetNamespace = schema.Attribute(XName.Get("targetNamespace"))?.Value;

                i++;
                result.Schemas.Add(new DdbXmlSchema
                {
                    Id = $"schema_{i}",
                    Namespace = targetNamespace,
                    Text = schema.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            result.Schemas.Clear();
            result.Schemas.Add(new DdbXmlSchema { Namespace = "", Text = collection.Text });
            result.SchemasError = ex.GetAllMessages();
        }

        return result;
    }

    private DdbRule CreateRule(Rule rule)
    {
        var result = InitBase(new DdbRule
        {
            TextBody = rule.TextBody
        }, rule);

        return result;
    }

    private DdbDefault CreateDefault(Default def)
    {
        var result = InitBase(new DdbDefault
        {
            TextBody = def.TextBody
        }, def);

        return result;
    }

    private DdbUserDefinedDataType CreateUserDefinedDataType(UserDefinedDataType udt)
    {
        // TODO: MaxLength vs. Storage (SSMS)?
        var result = InitBase(new DdbUserDefinedDataType
        {
            AllowIdentity = udt.AllowIdentity,
            Collation = udt.Collation,
            DefaultRef = !string.IsNullOrEmpty(udt.Default) ? CreateDefaultRef(udt.Parent, udt.DefaultSchema, udt.Default) : null,
            IsSchemaOwned = udt.IsSchemaOwned,
            Length = udt.Length == 0 || udt.Length == -1 ? null : udt.Length,
            MaxLength = udt.MaxLength == 0 || udt.MaxLength == -1 ? null : udt.MaxLength,
            IsNullable = udt.Nullable,
            NumericPrecision = udt.NumericPrecision,
            NumericScale = udt.NumericScale,
            Owner = udt.Owner,
            RuleRef = !string.IsNullOrEmpty(udt.Rule) ? CreateRuleRef(udt.Parent, udt.RuleSchema, udt.Rule) : null,
            SystemType = udt.SystemType,
            IsVariableLength = udt.VariableLength
        }, udt);

        return result;
    }

    private DdbSequence CreateSequence(Sequence sequence)
    {
        var result = InitBase(new DdbSequence
        {
            DataType = sequence.DataType.PrettyName(),
            DataTypeRef = ResolveDataType(sequence.Parent, sequence.DataType),
            IsCached = sequence.SequenceCacheType != SequenceCacheType.NoCache,
            CacheSize = sequence.CacheSize,
            IsSchemaOwned = sequence.IsSchemaOwned,
            IsCycleEnabled = sequence.IsCycleEnabled,
            IncrementValue = sequence.IncrementValue?.ToString(),
            StartValue = sequence.StartValue?.ToString(),
            MinValue = sequence.MinValue?.ToString(),
            MaxValue = sequence.MaxValue?.ToString(),
        }, sequence);

        return result;
    }

    private DdbUserDefinedTableType CreateUserDefinedTableType(UserDefinedTableType udt)
    {
        var result = InitBase(new DdbUserDefinedTableType
        {
            CreatedAt = udt.CreateDate,
            LastModifiedAt = udt.DateLastModified,
            Collation = udt.Collation,
            IsMemoryOptimized = udt.IsMemoryOptimized,
            IsUserDefined = udt.IsUserDefined,
            IsSchemaOwned = udt.IsSchemaOwned,
            Owner = udt.Owner,
            MaxLength = udt.MaxLength == 0 || udt.MaxLength == -1 ? null : udt.MaxLength,
            IsNullable = udt.Nullable,
        }, udt);

        foreach (Column column in udt.Columns)
        {
            result.Columns.Add(InitBase(CreateColumn<DdbTableColumn>(udt.Parent, column), column));
        }

        foreach (Check check in udt.Checks)
        {
            var c = InitBase(new DdbCheckConstraint
            {
                ConstraintText = check.Text,
                IsChecked = check.IsChecked,
                IsEnabled = check.IsEnabled
            }, check, noScript: true);
            result.Checks.Add(c);
        }

        AddIndex(udt, udt.Indexes, result);

        return result;
    }

    private DdbView CreateView(View view)
    {
        var result = InitBase(new DdbView
        {
            CreatedAt = view.CreateDate,
            LastModifiedAt = view.DateLastModified,
        }, view);

        foreach (Column column in view.Columns)
        {
            result.Columns.Add(InitBase(CreateColumn<DdbViewColumn>(view.Parent, column), column));
        }

        AddTriggers(view.Triggers, result);
        AddIndex(view, view.Indexes, result);

        return result;
    }

    private DdbTable CreateTable(Table table)
    {
        var result = InitBase(new DdbTable()
        {
            CreatedAt = table.CreateDate,
            LastModifiedAt = table.DateLastModified,
            FileGroup = table.Parent.FileGroups.FindFirstOrDefault<FileGroup>(table.FileGroup)?.ToNamedRef<DdbFileGroup>(),
            FileStreamGroup = table.Parent.FileGroups.FindFirstOrDefault<FileGroup>(table.FileStreamFileGroup)?.ToNamedRef<DdbFileGroup>(),
            TextFileGroup = table.Parent.FileGroups.FindFirstOrDefault<FileGroup>(table.TextFileGroup)?.ToNamedRef<DdbFileGroup>()
        }, table);

        if (table.IsPartitioned)
        {
            result.PartitionInfo.IsPartitioned = true;
            result.PartitionInfo.PartitionScheme = table.PartitionScheme;
            result.PartitionInfo.Columns.AddRange(table.PartitionSchemeParameters.OfType<PartitionSchemeParameter>().Select(x => x.Name));
        }

        if (table.IsFileTable)
        {
            result.FileTableInfo.IsFileTable = true;
            result.FileTableInfo.DirectoryName = table.FileTableDirectoryName;
            result.FileTableInfo.NameColumnCollation = table.FileTableNameColumnCollation;
            result.FileTableInfo.NamespaceEnabled = table.FileTableNamespaceEnabled;
        }

        foreach (ForeignKey foreignKey in table.ForeignKeys)
        {
            result.ForeignKeys.Add(InitBase(new DdbForeignKey
            {
                Columns = foreignKey.Columns.OfType<ForeignKeyColumn>().Select(c => CreateColumnRef(table, c.Name)).ToList(),
                IsChecked = foreignKey.IsChecked,
                ReferencedKey = foreignKey.ReferencedKey,
                ReferencedTable = CreateTableRef(table.Parent, foreignKey.ReferencedTableSchema, foreignKey.ReferencedTable)
            }, foreignKey, noScript: true));
        }

        foreach (Column column in table.Columns)
        {
            var ddbCol = InitBase(CreateColumn<DdbTableColumn>(table.Parent, column), column);
            if (ddbCol.IsForeignKey)
            {
                ddbCol.ForeignKeys = [.. result.ForeignKeys.Where(fk => fk.Columns.Any(c => c.Id == ddbCol.Id)).Select(fk => fk.ToNamedRef())];
            }

            result.Columns.Add(ddbCol);
        }

        foreach (Check check in table.Checks)
        {
            var c = InitBase(new DdbCheckConstraint
            {
                ConstraintText = check.Text,
                IsChecked = check.IsChecked,
                IsEnabled = check.IsEnabled
            }, check, noScript: true);
            result.Checks.Add(c);
        }

        AddTriggers(table.Triggers, result);
        AddIndex(table, table.Indexes, result);

        return result;
    }

    public void AddIndex<TColumn>(TableViewTableTypeBase tv, IndexCollection indexes, TabularDdbObject<TColumn> result) where TColumn : DdbColumnBase
    {
        foreach (SmoIndex index in indexes)
        {
            var idx = InitBase(new DdbIndex
            {
                IndexType = index.IndexType switch
                {
                    IndexType.ClusteredIndex => "CLUSTERED",
                    IndexType.NonClusteredIndex => "NONCLUSTERED",
                    IndexType.PrimaryXmlIndex => "PRIMARY XML",
                    IndexType.SecondaryXmlIndex => "XML",
                    IndexType.SpatialIndex => "SPATIAL",
                    IndexType.ClusteredColumnStoreIndex => "CLUSTERED COLUMNSTORE",
                    IndexType.NonClusteredColumnStoreIndex => "NONCLUSTERED COLUMNSTORE",
                    IndexType.NonClusteredHashIndex => "NONCLUSTERED HASH",
                    IndexType.SelectiveXmlIndex => "SELECTIVE XML",
                    IndexType.SecondarySelectiveXmlIndex => "SECONDARY SELECTIVE XML",
                    IndexType.HeapIndex => "HEAP",
                    _ => index.IndexType.ToString()
                },
                IndexKeyType = index.IndexKeyType switch
                {
                    IndexKeyType.DriPrimaryKey => "PRIMARY",
                    IndexKeyType.DriUniqueKey => "UNIQUE",
                    _ => ""
                },
                IsDisabled = index.IsDisabled,
                IsClustered = index.IsClustered,
                IsPartitioned = index.IsPartitioned,
                IsUnique = index.IsUnique,
                Filter = index.FilterDefinition,
                FileGroup = !string.IsNullOrEmpty(index.FileGroup) ? CreateFileGroupRef(tv.GetDatabase(), index.FileGroup) : null,
                FileStreamGroup = !string.IsNullOrEmpty(index.FileStreamFileGroup) ? CreateFileGroupRef(tv.GetDatabase(), index.FileStreamFileGroup) : null,
            }, index, noScript: true);

            foreach (IndexedColumn indexColumn in index.IndexedColumns)
            {
                idx.Columns.Add(InitBase(new DdbIndexColumn
                {
                    ColumnRef = CreateColumnRef(tv, indexColumn.Name),
                    IsDescending = indexColumn.Descending,
                    IsIncluded = indexColumn.IsIncluded,
                    ColumnStoreOrderOrdinal = indexColumn.ColumnStoreOrderOrdinal,
                }, indexColumn));
            }

            var general = new DdbOptionCategory("general");
            SetOption(index, general, "AutoRecomputeStatistics", i => !i.NoAutomaticRecomputation);
            SetOption(index, general, nameof(SmoIndex.IgnoreDuplicateKeys));
            idx.Options.Add(general);

            var storage = new DdbOptionCategory("storage");
            SetOption(index, storage, nameof(SmoIndex.SortInTempdb));
            SetOption(index, storage, nameof(SmoIndex.FillFactor));
            SetOption(index, storage, nameof(SmoIndex.PadIndex));
            idx.Options.Add(storage);

            var operation = new DdbOptionCategory("operation");
            SetOption(index, operation, nameof(SmoIndex.MaximumDegreeOfParallelism));
            SetOption(index, operation, nameof(SmoIndex.IsOptimizedForSequentialKey));
            idx.Options.Add(operation);

            var locks = new DdbOptionCategory("locks");
            SetOption(index, locks, "AllowPageLocks", i => !i.DisallowPageLocks);
            SetOption(index, locks, "AllowRowLocks", i => !i.DisallowRowLocks);
            idx.Options.Add(locks);

            var spatial = new DdbOptionCategory("spatial");
            SetOption(index, spatial, nameof(SmoIndex.IsSpatialIndex));
            if (index.IsSpatialIndex)
            {
                SetOption(index, spatial, nameof(SmoIndex.SpatialIndexType));
                SetOption(index, spatial, nameof(SmoIndex.Level1Grid));
                SetOption(index, spatial, nameof(SmoIndex.Level2Grid));
                SetOption(index, spatial, nameof(SmoIndex.Level3Grid));
                SetOption(index, spatial, nameof(SmoIndex.Level4Grid));
                SetOption(index, spatial, nameof(SmoIndex.BoundingBoxXMin));
                SetOption(index, spatial, nameof(SmoIndex.BoundingBoxXMax));
                SetOption(index, spatial, nameof(SmoIndex.BoundingBoxYMin));
                SetOption(index, spatial, nameof(SmoIndex.BoundingBoxYMax));
            }
            idx.Options.Add(spatial);

            var xml = new DdbOptionCategory("xml");
            SetOption(index, xml, nameof(SmoIndex.IsXmlIndex));
            if (index.IsXmlIndex)
            {
                SetOption(index, xml, nameof(SmoIndex.ParentXmlIndex));
                SetOption(index, xml, nameof(SmoIndex.SecondaryXmlIndexType));
            }
            idx.Options.Add(xml);

            // Miscellaneous Options
            var miscellaneous = new DdbOptionCategory("miscellaneous");
            SetOption(index, miscellaneous, nameof(SmoIndex.HasCompressedPartitions));
            SetOption(index, miscellaneous, nameof(SmoIndex.HasXmlCompressedPartitions));

            SetOption(index, miscellaneous, nameof(SmoIndex.CompressAllRowGroups));
            SetOption(index, miscellaneous, nameof(SmoIndex.CompactLargeObjects));
            SetOption(index, miscellaneous, nameof(SmoIndex.CompressionDelay));
            SetOption(index, miscellaneous, nameof(SmoIndex.DropExistingIndex));
            SetOption(index, miscellaneous, nameof(SmoIndex.HasSparseColumn));

            SetOption(index, miscellaneous, nameof(SmoIndex.IsFileTableDefined));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsFullTextKey));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsIndexOnComputed));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsIndexOnTable));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsMemoryOptimized));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsOnlineRebuildSupported));
            SetOption(index, miscellaneous, nameof(SmoIndex.IsOptimizedForSequentialKey));
            SetOption(index, miscellaneous, nameof(SmoIndex.LowPriorityAbortAfterWait));
            SetOption(index, miscellaneous, nameof(SmoIndex.LowPriorityMaxDuration));
            SetOption(index, miscellaneous, nameof(SmoIndex.LowPriorityMaxDuration));
            SetOption(index, miscellaneous, nameof(SmoIndex.OnlineIndexOperation));
            SetOption(index, miscellaneous, nameof(SmoIndex.ResumableIndexOperation));
            SetOption(index, miscellaneous, nameof(SmoIndex.ResumableMaxDuration));
            SetOption(index, miscellaneous, nameof(SmoIndex.ResumableOperationState));
            idx.Options.Add(miscellaneous);

            // TODO:
            //index.IndexedXmlPathName
            //index.IndexedXmlPathNamespaces
            //index.IndexedXmlPaths

            // TODO:
            //index.PartitionScheme
            //index.PartitionSchemeParameters
            //index.PhysicalPartitions


            result.Indexes.Add(idx);
        }
    }

    private void AddTriggers<TColumn>(TriggerCollection triggers, TabularDdbObject<TColumn> result) where TColumn : DdbColumnBase
    {
        foreach (Trigger trigger in triggers)
        {
            // TODO: When we handle NativeCompiled for SPs, etc. we need to handle it here as well.

            var c = InitBase(new DdbDmlTrigger
            {
                IsEnabled = trigger.IsEnabled,
                IsSchemaBound = trigger.IsSchemaBound,
                IsInsteadOf = trigger.InsteadOf,
                OnDelete = trigger.Delete,
                OnUpdate = trigger.Update,
                OnInsert = trigger.Insert,
            }, trigger, noScript: true);

            result.Triggers.Add(c);
        }
    }

    private NamedDdbRef CreateDefaultRef(Database database, string schemaName, string defaultName)
    {
        var def = database.FindDefaultByName(schemaName, defaultName);

        return new NamedDdbRef
        {
            Id = def.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbDefault>(isRef: true),
            Name = def.GetFullName()
        };
    }

    private NamedDdbRef CreateRuleRef(Database database, string schemaName, string ruleName)
    {
        var rule = database.FindRuleByName(schemaName, ruleName);

        return new NamedDdbRef
        {
            Id = rule.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbRule>(isRef: true),
            Name = rule.GetFullName()
        };
    }

    private NamedDdbRef CreateFileGroupRef(Database database, string fileGroupName)
    {
        var fileGroup = database.FindFileGroupByName(fileGroupName);
        return new NamedDdbRef
        {
            Id = fileGroup.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbFileGroup>(isRef: true),
            Name = fileGroup.Name
        };
    }

    private NamedDdbRef CreateColumnRef(TableViewTableTypeBase tableView, string columnName)
    {
        var column = tableView.FindColumnByName(columnName);
        return new NamedDdbRef
        {
            Id = column.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTableColumn>(isRef: true),
            Name = column.Name
        };
    }

    private NamedDdbRef CreateTableRef(Database database, string schemaName, string tableName)
    {
        var table = database.FindTableByName(schemaName, tableName);

        return new NamedDdbRef
        {
            Id = table.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTable>(isRef: true),
            Name = table.GetFullName()
        };
    }

    private T CreateColumn<T>(Database database, Column column) where T : DdbColumnBase, new()
    {
        return new T()
        {
            IsNullable = column.Nullable,
            IsComputed = column.Computed,
            ComputedText = column.ComputedText,
            DataType = column.DataType.PrettyName(),
            DataTypeRef = ResolveDataType(database, column.DataType),
            MaxLengthBytes = column.DataType.MaximumLength == -1 || column.DataType.MaximumLength == 0 ? null : column.DataType.MaximumLength,
            Precision = column.DataType.NumericPrecision,
            Scale = column.DataType.NumericScale,
            IsIdentity = column.Identity,
            IdentityIncrement = column.IdentityIncrement,
            IdentitySeed = column.IdentitySeed,
            Default = column.DefaultConstraint?.Text,
            InPrimaryKey = column.InPrimaryKey,
            IsForeignKey = column.IsForeignKey,
            Collation = column.Collation,
            IsFileStream = column.IsFileStream,
        };
    }

    private void SetOption<T>(Func<T> getSmoObject, DdbOptionCategory options, string propertyName,
       Func<T, object?>? getter = null)
       where T : SqlSmoObject
    {
        try
        {
            SetOption(getSmoObject(), options, propertyName, getter);
        }
        catch (UnsupportedVersionException)
        {
            string name = ModelCreatorExtensions.ToCamelCase(propertyName);
            options.Entries.Add(new(name, null, null));
        }
    }

    private void SetOption<T>(T smoObject, DdbOptionCategory options, string propertyName,
        Func<T, object?>? getter = null)
        where T : SqlSmoObject
    {
        object? value = null;
        string name = ModelCreatorExtensions.ToCamelCase(propertyName);
        if (getter != null)
        {
            value = getter(smoObject);
        }
        else
        {
            if (smoObject.IsSupportedProperty(propertyName))
            {
                try
                {
                    var prop = smoObject.Properties[propertyName];
                    value = prop == null || prop.IsNull ? null : prop.Value;
                }
                catch (PropertyCannotBeRetrievedException)
                {
                    // "Property ... is not available for Database '...'.
                    // This property may not exist for this object, or may not be retrievable due to insufficient access rights."
                }
            }
        }

        if (value != null)
        {
            if (value is bool b)
            {
                options.Entries.Add(new(name, b ? "true" : "false", "bool"));
            }
            else if (value is DateTime dt)
            {
                options.Entries.Add(new(name, dt.ToString("0"), "datetime"));
            }
            else if (value.GetType().IsEnum)
            {
                // Separate case, so that it isn't caught be next one (formattable && numeric)
                options.Entries.Add(new(name, value?.ToString(), "enum"));
            }
            else if (value is IFormattable formattable && value.GetType().IsNumeric())
            {
                options.Entries.Add(new(name, formattable.ToString(null, CultureInfo.InvariantCulture), "number"));
            }
            else
            {
                string typeName = "string";
                if (value is Guid)
                {
                    typeName = "guid";
                }

                options.Entries.Add(new(name, value?.ToString(), typeName));
            }
        }
        else
        {
            options.Entries.Add(new(name, null, null));
        }
    }

    private NamedDdbRef? ResolveDataType(Database database, DataType dataType)
    {
        NamedDdbRef? dataTypeRef = null;
        switch (dataType.SqlDataType)
        {
            case SqlDataType.UserDefinedDataType:
                dataTypeRef = database.UserDefinedDataTypes
                    .FindFirstOrDefault<UserDefinedDataType>(dataType.Schema, dataType.Name)?
                    .ToNamedRef<DdbUserDefinedDataType>();
                break;
            case SqlDataType.UserDefinedType:
                dataTypeRef = database.UserDefinedDataTypes
                    .FindFirstOrDefault<UserDefinedType>(dataType.Schema, dataType.Name)?
                    .ToNamedRef<DdbUserDefinedType>();
                break;
            case SqlDataType.UserDefinedTableType:
                dataTypeRef = database.UserDefinedTableTypes
                    .FindFirstOrDefault<UserDefinedTableType>(dataType.Schema, dataType.Name)?
                    .ToNamedRef<DdbUserDefinedTableType>();
                break;
        }
        return dataTypeRef;
    }
}
