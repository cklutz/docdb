﻿using DocDB.Contracts;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Globalization;
using System.Linq;
using SmoIndex = Microsoft.SqlServer.Management.Smo.Index;


namespace DocDB;

internal class ModelCreator
{
    public DdbObject? CreateObject(NamedSmoObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj switch
        {
            Database database => CreateDatabase(database),
            Schema schema => CreateSchema(schema),
            Table table => CreateTable(table),
            View view => CreateView(view),
            UserDefinedFunction udf => CreateUserDefinedFunction(udf),
            StoredProcedure sp => CreateStoredProcedure(sp),
            _ => throw new ArgumentOutOfRangeException(nameof(obj), obj.GetType(), null)
        };
    }

    private static T InitBase<T>(T obj, NamedSmoObject smo, bool noScript = false) where T : DdbObject
    {
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
                file.FileGroup = fg.ToRef();
                fg.Files.Add(file.ToRef());
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
        var result = InitBase(new DdbSchema(), schema);
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
            }
        }, udf);

        foreach (UserDefinedFunctionParameter parameter in udf.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbUserDefinedFunctionParameter()
            {
                DataType = parameter.DataType.PrettyName(),
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
            LastModifiedAt = sp.DateLastModified
        }, sp);

        
        foreach (StoredProcedureParameter parameter in sp.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbStoredProcedureParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                IsOutputParameter = parameter.IsOutputParameter,
                IsCursorParameter = parameter.IsCursorParameter,
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            }, parameter));
        }

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
            result.Columns.Add(InitBase(CreateColumn<DdbViewColumn>(column), column));
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
        }, table);

        if (table.IsPartitioned)
        {
            result.PartitionInfo.IsPartitioned = true;
            result.PartitionInfo.PartitionScheme = table.PartitionScheme;
            result.PartitionInfo.Columns.AddRange(table.PartitionSchemeParameters.OfType<PartitionSchemeParameter>().Select(x => x.Name));
            result.PartitionInfo.FileGroup = table.FileGroup;
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
            var ddbCol = InitBase(CreateColumn<DdbTableColumn>(column), column);
            if (ddbCol.IsForeignKey)
            {
                ddbCol.ForeignKeys = [.. result.ForeignKeys.Where(fk => fk.Columns.Any(c => c.Id == ddbCol.Id)).Select(fk => fk.ToRef())];
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

    public static void AddIndex<TColumn>(TableViewBase tableView, IndexCollection indexes, TabularDdbObject<TColumn> result) where TColumn : DdbColumnBase
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
                FileGroup = !string.IsNullOrEmpty(index.FileGroup) ? CreateFileGroupRef(tableView, index.FileGroup) : null,
                FileStreamGroup = !string.IsNullOrEmpty(index.FileStreamFileGroup) ? CreateFileGroupRef(tableView, index.FileStreamFileGroup) : null,
            }, index, noScript: true);

            foreach (IndexedColumn indexColumn in index.IndexedColumns)
            {
                idx.Columns.Add(InitBase(new DdbIndexColumn
                {
                    ColumnRef = CreateColumnRef(tableView, indexColumn.Name),
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

    private static void AddTriggers<TColumn>(TriggerCollection triggers, TabularDdbObject<TColumn> result) where TColumn : DdbColumnBase
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

    private static NamedDdbRef CreateFileGroupRef(TableViewBase tableView, string fileGroupName)
    {
        var fileGroup = tableView.GetDatabase().FindFileGroupByName(fileGroupName);
        return new NamedDdbRef
        {
            Id = fileGroup.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbFileGroup>(isRef: true),
            Name = fileGroup.Name
        };
    }

    private static NamedDdbRef CreateColumnRef(TableViewBase tableView, string columnName)
    {
        var column = tableView.FindColumnByName(columnName);
        return new NamedDdbRef
        {
            Id = column.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTableColumn>(isRef: true),
            Name = column.Name
        };
    }

    private static NamedDdbRef CreateTableRef(Database database, string schemaName, string tableName)
    {
        var table = database.FindTableByName(schemaName, tableName);

        return new NamedDdbRef
        {
            Id = table.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTable>(isRef: true),
            Name = table.GetFullName()
        };
    }

    private static T CreateColumn<T>(Column column) where T : DdbColumnBase, new()
    {
        return new T()
        {
            IsNullable = column.Nullable,
            IsComputed = column.Computed,
            ComputedText = column.ComputedText,
            DataType = column.DataType.PrettyName(),
            MaxLengthBytes = column.DataType.MaximumLength == -1 ? null : column.DataType.MaximumLength,
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

    private static void SetOption<T>(Func<T> getSmoObject, DdbOptionCategory options, string propertyName,
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

    private static void SetOption<T>(T smoObject, DdbOptionCategory options, string propertyName,
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
}
