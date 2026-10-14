SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('institution.fee_structures', 'U') IS NULL
BEGIN
    THROW 50002, 'The institution.fee_structures table must exist before applying this migration.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('institution.fee_structures')
      AND name = 'UQ_fee_structures_school_grade_term_year_category'
)
BEGIN
    ALTER TABLE institution.fee_structures
        DROP CONSTRAINT UQ_fee_structures_school_grade_term_year_category;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.fee_structures')
      AND name = 'UQ_fee_structures_school_grade_term_year_category_description'
)
BEGIN
    ALTER TABLE institution.fee_structures
        ADD CONSTRAINT UQ_fee_structures_school_grade_term_year_category_description
            UNIQUE (school_id, grade, term, academic_year, category, description);
END;

COMMIT TRANSACTION;
