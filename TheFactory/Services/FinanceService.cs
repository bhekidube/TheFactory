using System.Data;
using Microsoft.Data.SqlClient;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly SqlConnectionService _sqlConnectionService;

    public FinanceService(SqlConnectionService sqlConnectionService)
    {
        _sqlConnectionService = sqlConnectionService;
    }

    public async Task<FinanceDashboardDto> GetDashboardAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT l.Id, l.FirstName, l.Surname, l.Grade,
                     COALESCE(SUM(CASE WHEN ledger.[type] = 'DEBIT' AND ledger.is_voided = 0 THEN ledger.amount ELSE 0 END), 0) AS Billed,
                     COALESCE(SUM(CASE WHEN ledger.[type] = 'CREDIT' AND ledger.is_voided = 0 THEN ledger.amount ELSE 0 END), 0) AS Collected
              FROM institution.Learner AS l
              LEFT JOIN institution.student_ledgers AS ledger
                ON ledger.learner_id = l.Id AND ledger.school_id = l.TenantId
              WHERE l.TenantId = @SchoolId AND ISNULL(l.IsArchived, 0) = 0
              GROUP BY l.Id, l.FirstName, l.Surname, l.Grade
              ORDER BY l.FirstName, l.Surname, l.Id;",
            connection);
        AddInt(command, "@SchoolId", schoolId);

        var learners = new List<FinanceLearnerBalanceDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var billed = reader.GetDecimal(4);
            var collected = reader.GetDecimal(5);
            learners.Add(new FinanceLearnerBalanceDto
            {
                LearnerId = reader.GetInt32(0),
                FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Surname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Billed = billed,
                Collected = collected,
                Balance = billed - collected
            });
        }

        return new FinanceDashboardDto
        {
            TotalBilled = learners.Sum(learner => learner.Billed),
            TotalCollected = learners.Sum(learner => learner.Collected),
            OutstandingBalance = learners.Sum(learner => learner.Balance),
            Learners = learners
        };
    }

    public async Task<IReadOnlyCollection<StudentLedgerEntryDto>> GetLedgerAsync(
        int schoolId,
        int learnerId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT transaction_id, learner_id, [date], [type], category, amount,
                     running_balance, notes, reference_number, recorded_by, is_voided
              FROM institution.student_ledgers
              WHERE school_id = @SchoolId AND learner_id = @LearnerId
              ORDER BY [date], transaction_id;",
            connection);
        AddInt(command, "@SchoolId", schoolId);
        AddInt(command, "@LearnerId", learnerId);

        var ledger = new List<StudentLedgerEntryDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ledger.Add(MapLedgerEntry(reader));
        }

        return ledger;
    }

    public async Task<IReadOnlyCollection<FeeStructureDto>> GetFeeStructuresAsync(int schoolId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT fee_structure_id, school_id, grade, term, academic_year, category, description, amount
              FROM institution.fee_structures
              WHERE school_id = @SchoolId
              ORDER BY academic_year DESC, term, grade, category;",
            connection);
        AddInt(command, "@SchoolId", schoolId);

        var feeStructures = new List<FeeStructureDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            feeStructures.Add(MapFeeStructure(reader));
        }

        return feeStructures;
    }

    public async Task<FeeStructureDto> CreateFeeStructureAsync(
        int schoolId,
        FeeStructureUpsertRequest request,
        int recordedBy,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"INSERT INTO institution.fee_structures
                (school_id, grade, term, academic_year, category, description, amount, created_by)
              OUTPUT INSERTED.fee_structure_id
              VALUES (@SchoolId, @Grade, @Term, @AcademicYear, @Category, @Description, @Amount, @RecordedBy);",
            connection);
        AddInt(command, "@SchoolId", schoolId);
        AddString(command, "@Grade", request.Grade.Trim(), 100);
        AddString(command, "@Term", request.Term.Trim(), 40);
        AddInt(command, "@AcademicYear", request.AcademicYear);
        AddString(command, "@Category", request.Category.Trim(), 80);
        AddString(command, "@Description", request.Description.Trim(), 240);
        AddDecimal(command, "@Amount", request.Amount);
        AddInt(command, "@RecordedBy", recordedBy);
        var feeStructureId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

        return (await GetFeeStructuresAsync(schoolId, cancellationToken))
            .Single(feeStructure => feeStructure.FeeStructureId == feeStructureId);
    }

    public async Task<StudentLedgerEntryDto?> CreateTransactionAsync(
        int schoolId,
        int learnerId,
        LedgerTransactionUpsertRequest request,
        int recordedBy,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            if (!await LockLearnerAsync(connection, transaction, schoolId, learnerId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            int transactionId;
            using (var command = new SqlCommand(
                @"INSERT INTO institution.student_ledgers
                    (school_id, learner_id, [date], [type], category, amount, notes, reference_number, recorded_by)
                  OUTPUT INSERTED.transaction_id
                  VALUES (@SchoolId, @LearnerId, @Date, @Type, @Category, @Amount, @Notes, @ReferenceNumber, @RecordedBy);",
                connection,
                transaction))
            {
                AddLedgerRequestParameters(command, schoolId, learnerId, request);
                AddInt(command, "@RecordedBy", recordedBy);
                transactionId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            await RecalculateBalancesAsync(connection, transaction, schoolId, learnerId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetTransactionAsync(schoolId, transactionId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StudentLedgerEntryDto?> UpdateTransactionAsync(
        int schoolId,
        int transactionId,
        LedgerTransactionUpsertRequest request,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var learnerId = await GetTransactionLearnerIdAsync(connection, transaction, schoolId, transactionId, cancellationToken);
            if (!learnerId.HasValue || !await LockLearnerAsync(connection, transaction, schoolId, learnerId.Value, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            using (var command = new SqlCommand(
                @"UPDATE institution.student_ledgers
                  SET [date] = @Date, [type] = @Type, category = @Category, amount = @Amount,
                      notes = @Notes, reference_number = @ReferenceNumber, updated_by = @UpdatedBy,
                      updated_at = SYSUTCDATETIME()
                  WHERE transaction_id = @TransactionId AND school_id = @SchoolId AND is_voided = 0;",
                connection,
                transaction))
            {
                AddLedgerRequestParameters(command, schoolId, learnerId.Value, request);
                AddInt(command, "@UpdatedBy", updatedBy);
                AddInt(command, "@TransactionId", transactionId);
                if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            await RecalculateBalancesAsync(connection, transaction, schoolId, learnerId.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetTransactionAsync(schoolId, transactionId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> VoidTransactionAsync(
        int schoolId,
        int transactionId,
        int voidedBy,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var learnerId = await GetTransactionLearnerIdAsync(connection, transaction, schoolId, transactionId, cancellationToken);
            if (!learnerId.HasValue || !await LockLearnerAsync(connection, transaction, schoolId, learnerId.Value, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            using (var command = new SqlCommand(
                @"UPDATE institution.student_ledgers
                  SET is_voided = 1, voided_by = @VoidedBy, voided_at = SYSUTCDATETIME(),
                      void_reason = N'Voided by school finance user.', updated_at = SYSUTCDATETIME()
                  WHERE transaction_id = @TransactionId AND school_id = @SchoolId AND is_voided = 0;",
                connection,
                transaction))
            {
                AddInt(command, "@VoidedBy", voidedBy);
                AddInt(command, "@TransactionId", transactionId);
                AddInt(command, "@SchoolId", schoolId);
                if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await RecalculateBalancesAsync(connection, transaction, schoolId, learnerId.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BulkInvoiceResultDto?> GenerateInvoicesAsync(
        int schoolId,
        int feeStructureId,
        int recordedBy,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            string grade;
            using (var feeCommand = new SqlCommand(
                @"SELECT grade FROM institution.fee_structures WITH (UPDLOCK, HOLDLOCK)
                  WHERE fee_structure_id = @FeeStructureId AND school_id = @SchoolId;",
                connection,
                transaction))
            {
                AddInt(feeCommand, "@FeeStructureId", feeStructureId);
                AddInt(feeCommand, "@SchoolId", schoolId);
                var feeGrade = await feeCommand.ExecuteScalarAsync(cancellationToken);
                if (feeGrade is null || feeGrade == DBNull.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                grade = Convert.ToString(feeGrade) ?? string.Empty;
            }

            using var command = new SqlCommand(
                @"DECLARE @CreatedLearners TABLE (LearnerId INT NOT NULL);
                  INSERT INTO institution.student_ledgers
                    (school_id, learner_id, [date], [type], category, amount, notes, reference_number, fee_structure_id, recorded_by)
                  OUTPUT INSERTED.learner_id INTO @CreatedLearners (LearnerId)
                  SELECT @SchoolId, learner.Id, CONVERT(date, SYSUTCDATETIME()), 'DEBIT',
                         fee.category, fee.amount, fee.description,
                         CONCAT(N'INV-', fee.academic_year, N'-', fee.term, N'-', learner.Id),
                         fee.fee_structure_id, @RecordedBy
                  FROM institution.Learner AS learner WITH (UPDLOCK, HOLDLOCK)
                  INNER JOIN institution.fee_structures AS fee
                    ON fee.fee_structure_id = @FeeStructureId AND fee.school_id = @SchoolId
                  WHERE learner.TenantId = @SchoolId
                    AND learner.Grade = @Grade
                    AND ISNULL(learner.IsArchived, 0) = 0
                    AND NOT EXISTS (
                        SELECT 1 FROM institution.student_ledgers AS existing
                        WHERE existing.school_id = @SchoolId
                          AND existing.learner_id = learner.Id
                          AND existing.fee_structure_id = fee.fee_structure_id
                          AND existing.is_voided = 0
                    );
                  SELECT LearnerId FROM @CreatedLearners;",
                connection,
                transaction);
            AddInt(command, "@SchoolId", schoolId);
            AddInt(command, "@FeeStructureId", feeStructureId);
            AddInt(command, "@RecordedBy", recordedBy);
            AddString(command, "@Grade", grade, 100);

            var learnerIds = new List<int>();
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    learnerIds.Add(reader.GetInt32(0));
                }
            }

            foreach (var learnerId in learnerIds)
            {
                await RecalculateBalancesAsync(connection, transaction, schoolId, learnerId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new BulkInvoiceResultDto
            {
                InvoiceCount = learnerIds.Count,
                LearnerCount = learnerIds.Count,
                Message = learnerIds.Count == 0
                    ? "No new invoices were generated. Learners may already have an invoice for this fee structure."
                    : $"Generated {learnerIds.Count} invoice(s)."
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<StudentLedgerEntryDto?> GetTransactionAsync(int schoolId, int transactionId, CancellationToken cancellationToken)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT transaction_id, learner_id, [date], [type], category, amount,
                     running_balance, notes, reference_number, recorded_by, is_voided
              FROM institution.student_ledgers
              WHERE school_id = @SchoolId AND transaction_id = @TransactionId;",
            connection);
        AddInt(command, "@SchoolId", schoolId);
        AddInt(command, "@TransactionId", transactionId);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapLedgerEntry(reader) : null;
    }

    private static async Task<int?> GetTransactionLearnerIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int schoolId,
        int transactionId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT learner_id FROM institution.student_ledgers WITH (UPDLOCK, HOLDLOCK)
              WHERE school_id = @SchoolId AND transaction_id = @TransactionId;",
            connection,
            transaction);
        AddInt(command, "@SchoolId", schoolId);
        AddInt(command, "@TransactionId", transactionId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }

    private static async Task<bool> LockLearnerAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int schoolId,
        int learnerId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT Id FROM institution.Learner WITH (UPDLOCK, HOLDLOCK)
              WHERE Id = @LearnerId AND TenantId = @SchoolId AND ISNULL(IsArchived, 0) = 0;",
            connection,
            transaction);
        AddInt(command, "@LearnerId", learnerId);
        AddInt(command, "@SchoolId", schoolId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is not null && value != DBNull.Value;
    }

    private static async Task RecalculateBalancesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int schoolId,
        int learnerId,
        CancellationToken cancellationToken)
    {
        var entries = new List<(int TransactionId, string Type, decimal Amount, bool IsVoided)>();
        using (var command = new SqlCommand(
            @"SELECT transaction_id, [type], amount, is_voided
              FROM institution.student_ledgers WITH (UPDLOCK, HOLDLOCK)
              WHERE school_id = @SchoolId AND learner_id = @LearnerId
              ORDER BY [date], transaction_id;",
            connection,
            transaction))
        {
            AddInt(command, "@SchoolId", schoolId);
            AddInt(command, "@LearnerId", learnerId);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                entries.Add((reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetBoolean(3)));
            }
        }

        decimal balance = 0;
        foreach (var entry in entries)
        {
            if (!entry.IsVoided)
            {
                balance += entry.Type == "DEBIT" ? entry.Amount : -entry.Amount;
            }

            using var update = new SqlCommand(
                @"UPDATE institution.student_ledgers
                  SET running_balance = @Balance, updated_at = SYSUTCDATETIME()
                  WHERE transaction_id = @TransactionId AND school_id = @SchoolId;",
                connection,
                transaction);
            AddDecimal(update, "@Balance", balance);
            AddInt(update, "@TransactionId", entry.TransactionId);
            AddInt(update, "@SchoolId", schoolId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddLedgerRequestParameters(SqlCommand command, int schoolId, int learnerId, LedgerTransactionUpsertRequest request)
    {
        AddInt(command, "@SchoolId", schoolId);
        AddInt(command, "@LearnerId", learnerId);
        var date = command.Parameters.Add("@Date", SqlDbType.Date);
        date.Value = request.Date.Date;
        AddString(command, "@Type", request.Type.Trim().ToUpperInvariant(), 6, SqlDbType.VarChar);
        AddString(command, "@Category", request.Category.Trim(), 80);
        AddDecimal(command, "@Amount", request.Amount);
        AddNullableString(command, "@Notes", request.Notes, 500);
        AddNullableString(command, "@ReferenceNumber", request.ReferenceNumber, 80);
    }

    private static void AddInt(SqlCommand command, string name, int value) =>
        command.Parameters.Add(name, SqlDbType.Int).Value = value;

    private static void AddString(SqlCommand command, string name, string value, int size, SqlDbType type = SqlDbType.NVarChar) =>
        command.Parameters.Add(name, type, size).Value = value;

    private static void AddNullableString(SqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static void AddDecimal(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static StudentLedgerEntryDto MapLedgerEntry(SqlDataReader reader) => new()
    {
        TransactionId = reader.GetInt32(0),
        LearnerId = reader.GetInt32(1),
        Date = reader.GetDateTime(2),
        Type = reader.GetString(3),
        Category = reader.GetString(4),
        Amount = reader.GetDecimal(5),
        RunningBalance = reader.GetDecimal(6),
        Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
        ReferenceNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
        RecordedBy = reader.GetInt32(9),
        IsVoided = reader.GetBoolean(10)
    };

    private static FeeStructureDto MapFeeStructure(SqlDataReader reader) => new()
    {
        FeeStructureId = reader.GetInt32(0),
        SchoolId = reader.GetInt32(1),
        Grade = reader.GetString(2),
        Term = reader.GetString(3),
        AcademicYear = reader.GetInt32(4),
        Category = reader.GetString(5),
        Description = reader.GetString(6),
        Amount = reader.GetDecimal(7)
    };
}
