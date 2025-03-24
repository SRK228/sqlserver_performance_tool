using Microsoft.Data.SqlClient;
using System.Text;

class SchemaAnalyzer
{
    private readonly string connectionString;
    private readonly string outputPath;

    public SchemaAnalyzer(string connectionString, string outputPath)
    {
        this.connectionString = connectionString;
        this.outputPath = outputPath;
    }

    public async Task AnalyzeDatabase()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            
            // Generate reports
            await GenerateTableAnalysis(connection);
            await GenerateStoredProcedureAnalysis(connection);
            await GenerateIndexAnalysis(connection);
            await GenerateTableRelationships(connection);
            await GenerateTableUsageInStoredProcedures(connection);
        }
    }

    private async Task GenerateTableAnalysis(SqlConnection connection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Table Analysis Report");
        sb.AppendLine("\n## Overview");
        
        // Get table information
        using (var command = new SqlCommand(@"
            SELECT 
                t.name AS TableName,
                s.name AS SchemaName,
                SUM(p.rows) AS RecordCount,
                CAST(ROUND(SUM(a.total_pages) * 8.0 / 1024, 2) AS DECIMAL(10,2)) AS TotalSpaceMB,
                (
                    SELECT STRING_AGG(CONCAT(c.name, ' (', tp.name, 
                        CASE 
                            WHEN tp.name IN ('varchar', 'nvarchar', 'char', 'nchar') 
                            THEN CONCAT('(', c.max_length, ')')
                            ELSE ''
                        END, ')'), ', ')
                    FROM sys.columns c
                    JOIN sys.types tp ON c.system_type_id = tp.system_type_id
                    WHERE c.object_id = t.object_id
                ) AS ColumnList
            FROM sys.tables t
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            JOIN sys.indexes i ON t.object_id = i.object_id
            JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
            JOIN sys.allocation_units a ON p.partition_id = a.container_id
            WHERE t.is_ms_shipped = 0
            GROUP BY t.name, s.name, t.object_id
            ORDER BY TotalSpaceMB DESC", connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                sb.AppendLine("\n| Table Name | Schema | Row Count | Size (MB) | Columns |");
                sb.AppendLine("|------------|---------|-----------|-----------|---------|");
                
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"|{reader["TableName"]}|{reader["SchemaName"]}|{reader["RecordCount"]}|{reader["TotalSpaceMB"]}|{reader["ColumnList"]}|");
                }
            }
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "1_TableAnalysis.md"), sb.ToString());
    }

    private async Task GenerateStoredProcedureAnalysis(SqlConnection connection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Stored Procedure Analysis Report");
        sb.AppendLine("\n## Overview");

        using (var command = new SqlCommand(@"
            SELECT 
                p.name AS ProcedureName,
                OBJECT_DEFINITION(p.object_id) AS ProcedureDefinition,
                p.create_date,
                p.modify_date,
                (
                    SELECT COUNT(*) 
                    FROM sys.sql_dependencies d 
                    WHERE d.object_id = p.object_id
                ) AS DependencyCount
            FROM sys.procedures p
            WHERE p.is_ms_shipped = 0
            ORDER BY p.name", connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                sb.AppendLine("\n| Procedure Name | Created | Last Modified | Dependencies | Complexity Score |");
                sb.AppendLine("|----------------|---------|----------------|--------------|-----------------|");
                
                while (await reader.ReadAsync())
                {
                    string procDef = reader["ProcedureDefinition"]?.ToString() ?? "";
                    int complexityScore = CalculateComplexityScore(procDef);
                    
                    sb.AppendLine($"|{reader["ProcedureName"]}|{((DateTime)reader["create_date"]).ToShortDateString()}|{((DateTime)reader["modify_date"]).ToShortDateString()}|{reader["DependencyCount"]}|{complexityScore}|");
                }
            }
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "2_StoredProcedureAnalysis.md"), sb.ToString());
    }

    private async Task GenerateIndexAnalysis(SqlConnection connection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Index Analysis Report");
        sb.AppendLine("\n## Overview");

        using (var command = new SqlCommand(@"
            SELECT 
                OBJECT_NAME(i.object_id) AS TableName,
                i.name AS IndexName,
                i.type_desc AS IndexType,
                i.is_unique,
                i.is_primary_key,
                i.is_unique_constraint,
                ISNULL(usage.user_seeks, 0) as user_seeks,
                ISNULL(usage.user_scans, 0) as user_scans,
                ISNULL(usage.user_lookups, 0) as user_lookups,
                ISNULL(usage.user_updates, 0) as user_updates,
                CAST(ROUND(SUM(s.used_page_count) * 8.0 / 1024, 2) AS DECIMAL(10,2)) AS IndexSizeMB
            FROM sys.indexes i
            LEFT JOIN sys.dm_db_index_usage_stats usage 
                ON i.object_id = usage.object_id 
                AND i.index_id = usage.index_id
                AND usage.database_id = DB_ID()
            JOIN sys.dm_db_partition_stats s 
                ON i.object_id = s.object_id 
                AND i.index_id = s.index_id
            WHERE OBJECT_SCHEMA_NAME(i.object_id) != 'sys'
            GROUP BY 
                OBJECT_NAME(i.object_id), 
                i.name, 
                i.type_desc, 
                i.is_unique, 
                i.is_primary_key, 
                i.is_unique_constraint,
                usage.user_seeks,
                usage.user_scans,
                usage.user_lookups,
                usage.user_updates
            ORDER BY IndexSizeMB DESC", connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                sb.AppendLine("\n| Table Name | Index Name | Type | Unique | PK | Seeks | Scans | Lookups | Updates | Size (MB) |");
                sb.AppendLine("|------------|------------|------|---------|-------|--------|--------|----------|----------|------------|");
                
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"|{reader["TableName"]}|{reader["IndexName"]}|{reader["IndexType"]}|{reader["is_unique"]}|{reader["is_primary_key"]}|{reader["user_seeks"]}|{reader["user_scans"]}|{reader["user_lookups"]}|{reader["user_updates"]}|{reader["IndexSizeMB"]}|");
                }
            }
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "3_IndexAnalysis.md"), sb.ToString());
    }

    private async Task GenerateTableRelationships(SqlConnection connection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Table Relationships Analysis");
        sb.AppendLine("\n## Foreign Key Relationships");

        using (var command = new SqlCommand(@"
            SELECT 
                OBJECT_NAME(f.parent_object_id) AS TableName,
                COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
                OBJECT_NAME(f.referenced_object_id) AS ReferenceTableName,
                COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferenceColumnName,
                f.name AS ForeignKeyName,
                f.is_disabled
            FROM sys.foreign_keys f
            JOIN sys.foreign_key_columns fc ON f.object_id = fc.constraint_object_id
            ORDER BY TableName, ReferenceTableName", connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                sb.AppendLine("\n| Table | Column | References Table | References Column | FK Name | Disabled |");
                sb.AppendLine("|--------|---------|------------------|-------------------|----------|----------|");
                
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"|{reader["TableName"]}|{reader["ColumnName"]}|{reader["ReferenceTableName"]}|{reader["ReferenceColumnName"]}|{reader["ForeignKeyName"]}|{reader["is_disabled"]}|");
                }
            }
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "4_TableRelationships.md"), sb.ToString());
    }

    private async Task GenerateTableUsageInStoredProcedures(SqlConnection connection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Table Usage in Stored Procedures");
        sb.AppendLine("\n## Overview");

        using (var command = new SqlCommand(@"
            SELECT 
                OBJECT_NAME(d.referenced_major_id) AS TableName,
                COUNT(DISTINCT d.object_id) AS ProcedureCount,
                STRING_AGG(OBJECT_NAME(d.object_id), ', ') AS Procedures
            FROM sys.sql_dependencies d
            WHERE d.class = 1
            GROUP BY d.referenced_major_id
            ORDER BY ProcedureCount DESC", connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                sb.AppendLine("\n| Table Name | Procedure Count | Procedures |");
                sb.AppendLine("|------------|-----------------|------------|");
                
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"|{reader["TableName"]}|{reader["ProcedureCount"]}|{reader["Procedures"]}|");
                }
            }
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "5_TableUsageInSPs.md"), sb.ToString());
    }

    private int CalculateComplexityScore(string procedureDefinition)
    {
        if (string.IsNullOrEmpty(procedureDefinition)) return 0;
        
        int score = 0;
        
        // Count common complexity indicators
        score += procedureDefinition.Split("JOIN", StringSplitOptions.None).Length - 1;
        score += procedureDefinition.Split("CASE", StringSplitOptions.None).Length - 1;
        score += procedureDefinition.Split("CURSOR", StringSplitOptions.None).Length - 1;
        score += procedureDefinition.Split("WHILE", StringSplitOptions.None).Length - 1;
        score += procedureDefinition.Split("TRIGGER", StringSplitOptions.None).Length - 1;
        
        return score;
    }
}

class Program
{
    static async Task Main()
    {
        string connectionString = @"Server=SRK\SQLSERVER2;Database=CONSTRUCTION;Trusted_Connection=True;TrustServerCertificate=True;";
        string outputPath = "..\\DatabaseAnalysis";
        
        Console.WriteLine("Starting database analysis...");
        var analyzer = new SchemaAnalyzer(connectionString, outputPath);
        await analyzer.AnalyzeDatabase();
        Console.WriteLine("Analysis complete. Check the DatabaseAnalysis folder for reports.");
    }
} 