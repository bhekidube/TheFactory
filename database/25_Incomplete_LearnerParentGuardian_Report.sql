/*
    Lists learner parent/guardian records whose ParentFirstName is exactly one character.
    The appended marker ensures SQL Server LEN counts any trailing spaces in the value.
    This report is read-only.
*/

SELECT
   -- lpg.LearnerId,
   -- learner.TenantId,
    learner.FirstName AS LearnerFirstName,
    learner.Surname AS LearnerSurname,
    learner.Grade,
    address.AddressLine1,
    address.AddressLine2,
    address.City,
    address.ProvinceState,
    address.PostalCode,
    address.Country,
    lpg.ParentFirstName,
    lpg.ParentSurname,
    lpg.ParentPhoneNumber,
    lpg.ParentEmailAddress,
    lpg.RelationshipToLearner,
    lpg.UpdatedAt
FROM institution.LearnerParentGuardian AS lpg
LEFT JOIN institution.Learner AS learner
    ON learner.Id = lpg.LearnerId
LEFT JOIN institution.Addresses AS address
    ON address.AddressId = lpg.AddressId
--WHERE LEN(lpg.ParentFirstName + N'#') - 1 = 1
--WHERE PARENTPHONENUMBER = '0772937611'
--where AddressLine1 is null and AddressLine2 is null and City is null and ProvinceState is null and PostalCode is null and Country is null
ORDER BY
    learner.TenantId,
    learner.Surname,
    learner.FirstName,
    lpg.LearnerId;
