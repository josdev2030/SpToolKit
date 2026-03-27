// SpToolkit.Samples
// ==================
// Demonstrates end-to-end usage of SpToolkit:
//   1. Register IStoredProcedureExecutor via AddSpToolkit DI extension
//   2. Use hand-written request/response/row classes (same shape as generated .g.cs files)
//   3. Execute a stored procedure and query a result set
//
// To run against a real SQL Server:
//   - Set the connection string below (or via environment variable)
//   - Ensure the sample SPs exist in your database (DDL at the bottom of this file)
//
// Without SQL Server the project compiles and demonstrates the API shape.

using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Contracts;
using SpToolkit.Abstractions.Models;
using SpToolkit.Runtime.DependencyInjection;
using SpToolkit.Samples;

// ── Host setup ─────────────────────────────────────────────────────────────

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Option A: own connection string
        services.AddSpToolkit(opts =>
        {
            opts.ConnectionString = "Server=.;Database=SampleDb;Trusted_Connection=True;TrustServerCertificate=True;";
            opts.EnableLogging    = true;
            opts.LogParameterValues = false;
        });

        // Option B (EF Core): uncomment and add your DbContext
        // services.AddDbContext<AppDbContext>(...);
        // services.AddSpToolkit<AppDbContext>();

        services.AddScoped<SampleRunner>();
    })
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
        logging.AddConsole();
    })
    .Build();

await host.Services.GetRequiredService<SampleRunner>().RunAsync();

// ── Runner ─────────────────────────────────────────────────────────────────

namespace SpToolkit.Samples
{
    public sealed class SampleRunner
    {
        private readonly IStoredProcedureExecutor _sp;
        private readonly ILogger<SampleRunner> _logger;

        public SampleRunner(IStoredProcedureExecutor sp, ILogger<SampleRunner> logger)
        {
            _sp     = sp;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("=== SpToolkit Sample ===");

            // ── Sample 1: ExecuteAsync (SP with output parameters) ─────────────

            _logger.LogInformation("--- Sample 1: Create user ---");
            try
            {
                var createResponse = await _sp.ExecuteAsync<CreateUserRequest, CreateUserResponse>(
                    "dbo.SP_CREATE_USER",
                    new CreateUserRequest
                    {
                        Name  = "Josue",
                        Email = "josue@example.com",
                    });

                _logger.LogInformation(
                    "Created user with ID={UserId}, ReturnCode={Code}, Message={Msg}",
                    createResponse.UserId,
                    createResponse.ReturnCode,
                    createResponse.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sample 1 skipped (SP not found or connection unavailable)");
            }

            // ── Sample 2: QueryAsync (SP that returns a result set) ────────────

            _logger.LogInformation("--- Sample 2: Get users ---");
            try
            {
                var users = await _sp.QueryAsync<GetUsersRequest, UserRow>(
                    "dbo.SP_GET_USERS",
                    new GetUsersRequest { MaxRows = 10 });

                _logger.LogInformation("Found {Count} users", users.Count);

                foreach (var u in users)
                    _logger.LogInformation("  [{Id}] {Name} <{Email}>", u.UserId, u.Name, u.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sample 2 skipped (SP not found or connection unavailable)");
            }

            // ── Sample 3: QuerySingleAsync (SP that returns 0 or 1 row) ───────

            _logger.LogInformation("--- Sample 3: Get user by ID ---");
            try
            {
                var user = await _sp.QuerySingleAsync<GetUserByIdRequest, UserRow>(
                    "dbo.SP_GET_USER_BY_ID",
                    new GetUserByIdRequest { UserId = 1 });

                if (user is not null)
                    _logger.LogInformation("Found: [{Id}] {Name}", user.UserId, user.Name);
                else
                    _logger.LogInformation("User not found");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sample 3 skipped (SP not found or connection unavailable)");
            }

            _logger.LogInformation("=== Done ===");
        }
    }

    // ── Request / Response / Row classes ──────────────────────────────────
    // These mirror what SpToolkit.Generator.Cli would produce as .g.cs files.

    [SpProcedure("dbo.SP_CREATE_USER")]
    public sealed class CreateUserRequest
    {
        [SpInput("@NAME", SqlDbType.NVarChar, Size = 100)]
        public string Name { get; set; } = string.Empty;

        [SpInput("@EMAIL", SqlDbType.NVarChar, Size = 150)]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class CreateUserResponse
    {
        [SpOutput("@USER_ID", SqlDbType.Int)]
        public int UserId { get; set; }

        [SpOutput("@RETURN_CODE", SqlDbType.Int)]
        public int ReturnCode { get; set; }

        [SpOutput("@MESSAGE", SqlDbType.NVarChar, Size = 500)]
        public string Message { get; set; } = string.Empty;
    }

    [SpProcedure("dbo.SP_GET_USERS")]
    public sealed class GetUsersRequest
    {
        [SpInput("@MAX_ROWS", SqlDbType.Int)]
        public int MaxRows { get; set; } = 100;
    }

    [SpProcedure("dbo.SP_GET_USER_BY_ID")]
    public sealed class GetUserByIdRequest
    {
        [SpInput("@USER_ID", SqlDbType.Int)]
        public int UserId { get; set; }
    }

    public sealed class UserRow
    {
        [SpResultColumn("USER_ID")]
        public int UserId { get; set; }

        [SpResultColumn("NAME")]
        public string Name { get; set; } = string.Empty;

        [SpResultColumn("EMAIL")]
        public string? Email { get; set; }

        [SpResultColumn("CREATED_AT")]
        public DateTime CreatedAt { get; set; }
    }
}

/*
 * Sample SQL Server DDL -- run this in your SampleDb database to test end-to-end:
  
    CREATE DATABASE SampleDb;
    GO


    USE SampleDb;
    GO


    CREATE TABLE Users (
        USER_ID INT IDENTITY(1,1) PRIMARY KEY,
        NAME NVARCHAR(100) NOT NULL,
        EMAIL NVARCHAR(150) NULL,
        CREATED_AT DATETIME NOT NULL DEFAULT GETUTCDATE()
    );
    GO



    -- SP_CREATE_USER
    CREATE OR ALTER PROCEDURE dbo.SP_CREATE_USER
        @NAME         NVARCHAR(100),
        @EMAIL        NVARCHAR(150),
        @USER_ID      INT OUTPUT,
        @RETURN_CODE  INT OUTPUT,
        @MESSAGE      NVARCHAR(500) OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;

        INSERT INTO Users (NAME, EMAIL, CREATED_AT)
        VALUES (@NAME, @EMAIL, GETUTCDATE());
    
        SET @USER_ID    = SCOPE_IDENTITY();
        SET @RETURN_CODE = 0;
        SET @MESSAGE    = 'OK';
    END;
    GO

    -- SP_GET_USERS
    CREATE OR ALTER PROCEDURE dbo.SP_GET_USERS
        @MAX_ROWS INT = 100
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT TOP (@MAX_ROWS) 
            USER_ID, 
            NAME, 
            EMAIL, 
            CREATED_AT 
        FROM Users 
        ORDER BY USER_ID;
    END;
    GO

    -- SP_GET_USER_BY_ID
    CREATE OR ALTER PROCEDURE dbo.SP_GET_USER_BY_ID
        @USER_ID INT
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT 
            USER_ID, 
            NAME, 
            EMAIL, 
            CREATED_AT 
        FROM Users 
        WHERE USER_ID = @USER_ID;
    END;
    GO
 */
