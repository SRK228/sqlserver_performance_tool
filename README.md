# SQL Server Performance Analysis Tool

This tool helps analyze SQL Server database performance, identify optimization opportunities, and generate detailed reports for database optimization.

## Features

- Table size and structure analysis
- Index usage and recommendations
- Stored procedure complexity analysis
- Foreign key relationship analysis
- Performance optimization recommendations
- Detailed optimization examples and implementation plan

## Prerequisites

- .NET 9.0 SDK or later
- SQL Server 2016 or later
- Windows OS (for running the tool)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/SRK228/sqlserver_performance_tool.git
cd sqlserver_performance_tool
```

2. Build the solution:
```bash
dotnet build
```

3. Configure the connection string:
   - Open `SchemaAnalyzer.cs`
   - Locate the connection string in the `Main` method
   - Update the connection string with your SQL Server details:
```csharp
string connectionString = @"Server=YOUR_SERVER_NAME;Database=YOUR_DATABASE_NAME;Trusted_Connection=True;TrustServerCertificate=True;";
```

## Usage

1. Run the analysis tool:
```bash
dotnet run
```

2. The tool will generate the following reports in the `DatabaseAnalysis` folder:
   - `1_TableAnalysis.md`: Table sizes and structure
   - `2_StoredProcedureAnalysis.md`: Stored procedure analysis
   - `3_IndexAnalysis.md`: Index usage and recommendations
   - `4_TableRelationships.md`: Foreign key relationships
   - `5_TableUsageInSPs.md`: Table usage in stored procedures
   - `OptimizationExamples.md`: Specific optimization examples
   - `ImplementationPlan.md`: Detailed implementation steps
   - `OptimizationRoadmap.md`: Overall optimization strategy

## Connection String Configuration

The tool uses SQL Server authentication. Update the connection string in `SchemaAnalyzer.cs` with your server details:

```csharp
// Example connection strings:

// Windows Authentication
string connectionString = @"Server=YOUR_SERVER_NAME;Database=YOUR_DATABASE_NAME;Trusted_Connection=True;TrustServerCertificate=True;";

// SQL Server Authentication
string connectionString = @"Server=YOUR_SERVER_NAME;Database=YOUR_DATABASE_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;";
```

## Generated Reports

### 1. Table Analysis Report
- Table sizes and row counts
- Column definitions
- Space usage
- Schema information

### 2. Stored Procedure Analysis
- List of stored procedures
- Creation and modification dates
- Complexity analysis
- Dependencies

### 3. Index Analysis
- Index types and sizes
- Usage statistics
- Performance metrics
- Recommendations for optimization

### 4. Table Relationships
- Foreign key relationships
- Table dependencies
- Data integrity constraints

### 5. Stored Procedure Usage
- Table usage in stored procedures
- Procedure dependencies
- Usage patterns

## Optimization Examples

The tool provides specific examples for:
1. Data Type Optimization
2. Primary Key Implementation
3. Index Implementation
4. Foreign Key Implementation

Each example includes:
- Current structure
- Proposed changes
- Expected improvements
- Risk assessment

## Implementation Plan

The tool generates a detailed implementation plan covering:
1. Pre-Implementation Phase
2. Data Type Optimization
3. Primary Key Implementation
4. Index Implementation
5. Foreign Key Implementation
6. Post-Implementation Phase

## Troubleshooting

1. **Connection Issues**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure proper permissions

2. **Report Generation Issues**
   - Check write permissions in output directory
   - Verify sufficient disk space
   - Check SQL Server permissions

3. **Performance Issues**
   - Monitor SQL Server resources
   - Check for blocking queries
   - Verify database size limits

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, please create an issue in the GitHub repository or contact the maintainers. 