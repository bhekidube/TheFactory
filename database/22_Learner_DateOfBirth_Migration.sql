IF COL_LENGTH('institution.Learner', 'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE institution.Learner
        ADD DateOfBirth DATE NULL;
END;
