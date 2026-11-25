using Microsoft.Data.SqlClient;

namespace SkillsSwap.Tests;

[Collection("SQL Server integration serial")]
public sealed class SqlServerPhase04CRConcurrencyTests
{
    [SqlServerPhase04Fact]
    public async Task New_unique_indexes_and_conditional_resolution_hold_with_independent_transactions()
    {
        await using var setup = await OpenAsync();
        var request = Convert.ToInt64(await ScalarAsync(setup, "SELECT Id FROM dbo.LearningRequests WHERE Message='04B legacy 1'"));
        Assert.True(request > 0);
        Assert.Equal(0, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.LearningSessionRequests WHERE LearningRequestId=@request", request));
        var session = Convert.ToInt64(await ScalarAsync(setup, """
            INSERT dbo.LearningSessions(LearningRequestId,StartsAt,DurationMinutes,Status,CreatedAtUtc,Title)
            OUTPUT inserted.Id VALUES(@request,'2090-01-02T09:00:00+00:00',30,0,'2090-01-01T09:00:00+00:00','04CR concurrent fixture');
            """, request));
        try
        {
            const string insertRequest = """
                INSERT dbo.LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status)
                SELECT Id,LearnerId,'2090-01-01T09:00:00+00:00',0 FROM dbo.LearningRequests WHERE Id=@request;
                """;
            const string insertAbsence = """
                INSERT dbo.SessionAbsenceRequests(LearningSessionId,RequestedByUserId,Reason,CreatedAtUtc,RequestedForStartsAtUtc,Status)
                SELECT @session,LearnerId,'04CR concurrent absence','2090-01-01T09:00:00+00:00','2090-01-02T09:00:00+00:00',0
                FROM dbo.LearningRequests WHERE Id=@request;
                """;
            const string fulfill = """
                UPDATE a SET Status=1,FulfilledSessionId=@session,ResolvedAtUtc='2090-01-01T09:10:00+00:00',ResolvedByUserId=r.TeacherId
                FROM dbo.LearningSessionRequests a JOIN dbo.LearningRequests r ON r.Id=a.LearningRequestId
                WHERE a.LearningRequestId=@request AND a.Status=0;
                """;
            const string insertFulfilled = """
                INSERT dbo.LearningSessionRequests(LearningRequestId,RequestedByUserId,CreatedAtUtc,Status,FulfilledSessionId,ResolvedAtUtc,ResolvedByUserId)
                SELECT Id,LearnerId,'2090-01-01T09:00:00+00:00',1,@session,'2090-01-01T09:10:00+00:00',TeacherId
                FROM dbo.LearningRequests WHERE Id=@request;
                """;
            async Task RaceAsync(string sql, int losingResult)
            {
                var bothReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var ready = 0;
                async Task<int> RunAsync()
                {
                    await using var connection = await OpenAsync();
                    await using var tx = (SqlTransaction)await connection.BeginTransactionAsync();
                    if (Interlocked.Increment(ref ready) == 2) bothReady.TrySetResult();
                    await bothReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    await using var command = Command(connection, sql, request, session);
                    command.Transaction = tx;
                    try
                    {
                        var count = await command.ExecuteNonQueryAsync();
                        await tx.CommitAsync();
                        return count;
                    }
                    catch (SqlException ex) when (ex.Number is 2601 or 2627)
                    {
                        await tx.RollbackAsync();
                        return -1;
                    }
                }
                var results = await Task.WhenAll(RunAsync(), RunAsync());
                Assert.Single(results, count => count == 1);
                Assert.Single(results, count => count == losingResult);
            }

            await RaceAsync(insertRequest, -1);
            Assert.Equal(1, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.LearningSessionRequests WHERE LearningRequestId=@request AND Status=0", request));
            await RaceAsync(insertAbsence, -1);
            Assert.Equal(1, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.SessionAbsenceRequests WHERE LearningSessionId=@session AND Status=0", request, session));
            await RaceAsync(fulfill, 0);
            Assert.Equal(1, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.LearningSessionRequests WHERE FulfilledSessionId=@session", request, session));
            await ScalarAsync(setup, "DELETE FROM dbo.LearningSessionRequests WHERE FulfilledSessionId=@session", request, session);
            await RaceAsync(insertFulfilled, -1);
            Assert.Equal(1, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.LearningSessionRequests WHERE FulfilledSessionId=@session", request, session));

            await using var tx = (SqlTransaction)await setup.BeginTransactionAsync();
            await using var pending = Command(setup, insertRequest, request, session);
            pending.Transaction = tx;
            Assert.Equal(1, await pending.ExecuteNonQueryAsync());
            await using var failure = Command(setup, insertAbsence, request, session);
            failure.Transaction = tx;
            await Assert.ThrowsAsync<SqlException>(() => failure.ExecuteNonQueryAsync());
            await tx.RollbackAsync();
            Assert.Equal(0, await ScalarAsync(setup, "SELECT COUNT(*) FROM dbo.LearningSessionRequests WHERE LearningRequestId=@request AND Status=0", request));
        }
        finally
        {
            await ScalarAsync(setup, """
                DELETE FROM dbo.SessionAbsenceRequests WHERE LearningSessionId=@session;
                DELETE FROM dbo.LearningSessionRequests WHERE FulfilledSessionId=@session
                    OR (LearningRequestId=@request AND CreatedAtUtc='2090-01-01T09:00:00+00:00' AND Status=0);
                DELETE FROM dbo.LearningSessions WHERE Id=@session AND Title='04CR concurrent fixture';
                """, request, session);
        }
    }

    private static async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection("Server=localhost;Database=SkillsSwap_Phase04_Integration;Integrated Security=True;TrustServerCertificate=True");
        try
        {
            await connection.OpenAsync();
            await ScalarAsync(connection, """
                IF DB_NAME() <> N'SkillsSwap_Phase04_Integration'
                   OR CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)) <> N'DESKTOP-70O9F5H'
                    THROW 51000, 'Integration identity guard', 1;
                IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261125193300_Phase04CRDirectSchedulingFoundation')
                    THROW 51000, 'Candidate migration required', 1;
                """);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static SqlCommand Command(SqlConnection connection, string sql, long request, long session)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add("@request", System.Data.SqlDbType.BigInt).Value = request;
        command.Parameters.Add("@session", System.Data.SqlDbType.BigInt).Value = session;
        return command;
    }

    private static async Task<object?> ScalarAsync(SqlConnection connection, string sql, long request = 0, long session = 0)
    {
        await using var command = Command(connection, sql, request, session);
        return await command.ExecuteScalarAsync();
    }
}
