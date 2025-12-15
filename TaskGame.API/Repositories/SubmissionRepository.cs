using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public SubmissionRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<TaskSubmission?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ts.""Id"", ts.""TaskAssignmentId"", ts.""SubmittedByUserId"", ts.""Notes"", ts.""SubmittedAt"",
                   ta.""Id"", ta.""TaskId"", ta.""AssignedToUserId"", ta.""AssignedAt"", ta.""Status"", ta.""CompletedAt"",
                   t.""Id"", t.""Title"", t.""Description"", t.""CreatedAt"", t.""DueDate"",
                   t.""CreatedByUserId"", t.""Priority"", t.""MaxDailyAssignments"",
                   sf.""Id"", sf.""TaskSubmissionId"", sf.""FileName"", sf.""FilePath"", 
                   sf.""ContentType"", sf.""FileSize"", sf.""UploadedAt""
            FROM ""TaskSubmissions"" ts
            INNER JOIN ""TaskAssignments"" ta ON ts.""TaskAssignmentId"" = ta.""Id""
            INNER JOIN ""Tasks"" t ON ta.""TaskId"" = t.""Id""
            LEFT JOIN ""SubmissionFiles"" sf ON ts.""Id"" = sf.""TaskSubmissionId""
            WHERE ts.""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var submissionDict = new Dictionary<Guid, TaskSubmission>();

        var submissions = await connection.QueryAsync<TaskSubmission, TaskAssignment, TaskItem, SubmissionFile, TaskSubmission>(
            sql,
            (submission, assignment, task, file) =>
            {
                if (!submissionDict.TryGetValue(submission.Id, out var submissionEntry))
                {
                    submissionEntry = submission;
                    submissionEntry.TaskAssignment = assignment;
                    submissionEntry.TaskAssignment.Task = task;
                    submissionEntry.Files = new List<SubmissionFile>();
                    submissionDict.Add(submission.Id, submissionEntry);
                }

                if (file != null)
                {
                    submissionEntry.Files.Add(file);
                }

                return submissionEntry;
            },
            new { Id = id },
            splitOn: "Id,Id,Id"
        );

        return submissionDict.Values.FirstOrDefault();
    }

    public async Task<TaskSubmission?> GetByAssignmentIdAsync(Guid assignmentId)
    {
        const string sql = @"
            SELECT ""Id"", ""TaskAssignmentId"", ""SubmittedByUserId"", ""Notes"", ""SubmittedAt""
            FROM ""TaskSubmissions""
            WHERE ""TaskAssignmentId"" = @AssignmentId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TaskSubmission>(sql, new { AssignmentId = assignmentId });
    }
    
    public async Task<TaskSubmission?> GetByAssignmentIdWithFilesAsync(Guid assignmentId)
    {
        const string sql = @"
            SELECT ts.""Id"", ts.""TaskAssignmentId"", ts.""SubmittedByUserId"", ts.""Notes"", ts.""SubmittedAt"",
                   sf.""Id"", sf.""TaskSubmissionId"", sf.""FileName"", sf.""FilePath"", 
                   sf.""ContentType"", sf.""FileSize"", sf.""UploadedAt""
            FROM ""TaskSubmissions"" ts
            LEFT JOIN ""SubmissionFiles"" sf ON ts.""Id"" = sf.""TaskSubmissionId""
            WHERE ts.""TaskAssignmentId"" = @AssignmentId";

        using var connection = _dbConnection.CreateConnection();
        TaskSubmission? submission = null;

        await connection.QueryAsync<TaskSubmission, SubmissionFile, TaskSubmission>(
            sql,
            (sub, file) =>
            {
                submission ??= new TaskSubmission
                {
                    Id = sub.Id,
                    TaskAssignmentId = sub.TaskAssignmentId,
                    SubmittedByUserId = sub.SubmittedByUserId,
                    Notes = sub.Notes,
                    SubmittedAt = sub.SubmittedAt,
                    Files = new List<SubmissionFile>()
                };

                if (file != null)
                {
                    submission.Files.Add(file);
                }

                return submission;
            },
            new { AssignmentId = assignmentId },
            splitOn: "Id"
        );

        if (submission == null)
        {
            // No submission at all; ensure files collection is empty
            var subOnly = await GetByAssignmentIdAsync(assignmentId);
            if (subOnly != null)
            {
                subOnly.Files = new List<SubmissionFile>();
                return subOnly;
            }
        }

        return submission;
    }

    public async Task<List<TaskSubmission>> GetAllCompletedAsync(Guid userId)
    {
        const string sql = @"
            SELECT ts.""Id"", ts.""TaskAssignmentId"", ts.""SubmittedByUserId"", ts.""Notes"", ts.""SubmittedAt"",
                   ta.""Id"", ta.""TaskId"", ta.""AssignedToUserId"", ta.""AssignedAt"", ta.""Status"", ta.""CompletedAt"",
                   t.""Id"", t.""Title"", t.""Description"", t.""CreatedAt"", t.""DueDate"",
                   t.""CreatedByUserId"", t.""Priority"", t.""MaxDailyAssignments"", t.""EventId"",
                   u.""Id"", u.""Username"", u.""Email"",
                   creator.""Id"", creator.""Username"", creator.""Email"",
                   e.""Id"", e.""Name"",e.""Description"", e.""StartDate"", e.""EndDate"",
                   sf.""Id"", sf.""TaskSubmissionId"", sf.""FileName"", sf.""FilePath"", 
                   sf.""ContentType"", sf.""FileSize"", sf.""UploadedAt""
            FROM ""TaskAssignments"" ta
				   INNER JOIN ""Tasks"" t ON ta.""TaskId"" = t.""Id"" 
				   INNER JOIN ""EventInvitations"" ei ON ei.""UserId"" = t.""CreatedByUserId""  
				   INNER JOIN ""Events"" e ON ei.""EventId"" = e.""Id"" 
				   INNER JOIN ""Users"" creator ON t.""CreatedByUserId"" = creator.""Id""
				   INNER JOIN ""Users"" u ON ta.""AssignedToUserId"" = u.""Id""
				   LEFT OUTER JOIN ""TaskSubmissions"" ts ON ts.""TaskAssignmentId"" = ta.""Id""
            LEFT OUTER JOIN ""SubmissionFiles"" sf ON ts.""Id"" = sf.""TaskSubmissionId""
            WHERE  ei.""UserId"" = @userId
            ORDER BY ts.""SubmittedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var submissionDict = new Dictionary<Guid, TaskSubmission>();

        await connection.QueryAsync<TaskSubmission, TaskAssignment, TaskItem, User, User, Event, SubmissionFile, TaskSubmission>(
            sql,
            (submission, assignment, task, submittedBy, creator, evt, file) =>
            {
                if (!submissionDict.TryGetValue(submission.Id, out var submissionEntry))
                {
                    submissionEntry = submission;
                    submissionEntry.TaskAssignment = assignment;
                    submissionEntry.TaskAssignment.Task = task;
                    submissionEntry.SubmittedBy = submittedBy;
                    task.CreatedBy = creator;
                    if (evt != null)
                    {
                        task.Event = evt;
                    }
                    submissionEntry.Files = new List<SubmissionFile>();
                    submissionDict.Add(submission.Id, submissionEntry);
                }

                if (file != null)
                {
                    submissionEntry.Files.Add(file);
                }

                return submissionEntry;
            },
            new { userId = userId },
            splitOn: "Id,Id,Id,Id,Id,Id"
        );

        return submissionDict.Values.ToList();
    }

    public async Task<Guid> CreateAsync(TaskSubmission submission)
    {
        const string sql = @"
            INSERT INTO ""TaskSubmissions"" (""Id"", ""TaskAssignmentId"", ""SubmittedByUserId"", ""Notes"", ""SubmittedAt"")
            VALUES (@Id, @TaskAssignmentId, @SubmittedByUserId, @Notes, @SubmittedAt)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, submission);
    }

    public async Task<Guid> CreateFileAsync(SubmissionFile file)
    {
        const string sql = @"
            INSERT INTO ""SubmissionFiles"" (""Id"", ""TaskSubmissionId"", ""FileName"", ""FilePath"", 
                                            ""ContentType"", ""FileSize"", ""UploadedAt"")
            VALUES (@Id, @TaskSubmissionId, @FileName, @FilePath, @ContentType, @FileSize, @UploadedAt)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, file);
    }

    public async Task<SubmissionFile?> GetFileByIdAsync(Guid fileId)
    {
        const string sql = @"
            SELECT ""Id"", ""TaskSubmissionId"", ""FileName"", ""FilePath"", ""ContentType"", ""FileSize"", ""UploadedAt""
            FROM ""SubmissionFiles""
            WHERE ""Id"" = @FileId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SubmissionFile>(sql, new { FileId = fileId });
    }
    
    public async Task<List<SubmissionFile>> GetFilesBySubmissionIdAsync(Guid submissionId)
    {
        const string sql = @"
            SELECT ""Id"", ""TaskSubmissionId"", ""FileName"", ""FilePath"", ""ContentType"", ""FileSize"", ""UploadedAt""
            FROM ""SubmissionFiles""
            WHERE ""TaskSubmissionId"" = @SubmissionId";

        using var connection = _dbConnection.CreateConnection();
        var files = await connection.QueryAsync<SubmissionFile>(sql, new { SubmissionId = submissionId });
        return files.ToList();
    }

    public async Task<int> DeleteFilesBySubmissionIdAsync(Guid submissionId)
    {
        const string sql = @"DELETE FROM ""SubmissionFiles"" WHERE ""TaskSubmissionId"" = @SubmissionId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteAsync(sql, new { SubmissionId = submissionId });
    }

    public async Task<bool> DeleteAsync(Guid submissionId)
    {
        const string sql = @"DELETE FROM ""TaskSubmissions"" WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = submissionId });
        return rowsAffected > 0;
    }
}
