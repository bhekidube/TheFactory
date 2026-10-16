IF COL_LENGTH('institution.Learner', 'LearnerType') IS NULL
BEGIN
    ALTER TABLE institution.Learner
        ADD LearnerType NVARCHAR(20) NOT NULL
            CONSTRAINT DF_institution_Learner_LearnerType DEFAULT ('FullDay');
END;
