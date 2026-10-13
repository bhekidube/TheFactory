SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Tenant', 'U') IS NULL
   OR OBJECT_ID('institution.Learner', 'U') IS NULL
   OR OBJECT_ID('dbo.User', 'U') IS NULL
BEGIN
    THROW 50001, 'The Tenant, Learner, and dbo.User tables must exist before applying the finance migration.', 1;
END;

IF OBJECT_ID('institution.fee_structures', 'U') IS NULL
BEGIN
    CREATE TABLE institution.fee_structures (
        fee_structure_id INT IDENTITY(1,1) NOT NULL,
        school_id INT NOT NULL,
        grade NVARCHAR(100) NOT NULL,
        term NVARCHAR(40) NOT NULL,
        academic_year INT NOT NULL,
        category NVARCHAR(80) NOT NULL,
        description NVARCHAR(240) NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        created_by INT NOT NULL,
        created_at DATETIME2(0) NOT NULL CONSTRAINT DF_fee_structures_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_fee_structures PRIMARY KEY (fee_structure_id),
        CONSTRAINT FK_fee_structures_school FOREIGN KEY (school_id) REFERENCES institution.Tenant (Id),
        CONSTRAINT FK_fee_structures_user FOREIGN KEY (created_by) REFERENCES dbo.[User] (UserId),
        CONSTRAINT CK_fee_structures_amount CHECK (amount > 0),
        CONSTRAINT CK_fee_structures_academic_year CHECK (academic_year BETWEEN 2000 AND 2100),
        CONSTRAINT UQ_fee_structures_school_grade_term_year_category
            UNIQUE (school_id, grade, term, academic_year, category)
    );
END;

IF OBJECT_ID('institution.student_ledgers', 'U') IS NULL
BEGIN
    CREATE TABLE institution.student_ledgers (
        transaction_id INT IDENTITY(1,1) NOT NULL,
        school_id INT NOT NULL,
        learner_id INT NOT NULL,
        [date] DATE NOT NULL,
        [type] VARCHAR(6) NOT NULL,
        category NVARCHAR(80) NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        running_balance DECIMAL(18,2) NOT NULL CONSTRAINT DF_student_ledgers_running_balance DEFAULT (0),
        notes NVARCHAR(500) NULL,
        reference_number NVARCHAR(80) NULL,
        fee_structure_id INT NULL,
        recorded_by INT NOT NULL,
        updated_by INT NULL,
        is_voided BIT NOT NULL CONSTRAINT DF_student_ledgers_is_voided DEFAULT (0),
        voided_by INT NULL,
        voided_at DATETIME2(0) NULL,
        void_reason NVARCHAR(240) NULL,
        created_at DATETIME2(0) NOT NULL CONSTRAINT DF_student_ledgers_created_at DEFAULT (SYSUTCDATETIME()),
        updated_at DATETIME2(0) NOT NULL CONSTRAINT DF_student_ledgers_updated_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_student_ledgers PRIMARY KEY (transaction_id),
        CONSTRAINT FK_student_ledgers_school FOREIGN KEY (school_id) REFERENCES institution.Tenant (Id),
        CONSTRAINT FK_student_ledgers_learner FOREIGN KEY (learner_id) REFERENCES institution.Learner (Id),
        CONSTRAINT FK_student_ledgers_fee_structure FOREIGN KEY (fee_structure_id) REFERENCES institution.fee_structures (fee_structure_id),
        CONSTRAINT FK_student_ledgers_recorded_by FOREIGN KEY (recorded_by) REFERENCES dbo.[User] (UserId),
        CONSTRAINT FK_student_ledgers_updated_by FOREIGN KEY (updated_by) REFERENCES dbo.[User] (UserId),
        CONSTRAINT FK_student_ledgers_voided_by FOREIGN KEY (voided_by) REFERENCES dbo.[User] (UserId),
        CONSTRAINT CK_student_ledgers_type CHECK ([type] IN ('DEBIT', 'CREDIT')),
        CONSTRAINT CK_student_ledgers_amount CHECK (amount > 0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.student_ledgers')
      AND name = 'IX_student_ledgers_school_learner_date'
)
BEGIN
    CREATE INDEX IX_student_ledgers_school_learner_date
        ON institution.student_ledgers (school_id, learner_id, [date], transaction_id)
        INCLUDE ([type], amount, running_balance, is_voided);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.student_ledgers')
      AND name = 'UX_student_ledgers_fee_invoice'
)
BEGIN
    CREATE UNIQUE INDEX UX_student_ledgers_fee_invoice
        ON institution.student_ledgers (school_id, learner_id, fee_structure_id)
        WHERE fee_structure_id IS NOT NULL AND is_voided = 0;
END;

COMMIT TRANSACTION;
