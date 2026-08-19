-- Optional schema migration note for persisting Work Detail comments in institution.Mark.
-- Run this only if your institution.Mark table does not already have a suitable text column.

IF COL_LENGTH('institution.Mark', 'Comment') IS NULL
BEGIN
    ALTER TABLE institution.Mark
    ADD Comment NVARCHAR(500) NULL;
END;

-- If you prefer to reuse the existing TeacherComments column, no schema change is required.
