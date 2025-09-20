-- Insert UserRoles only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'SystemAdmin')
    INSERT INTO UserRole (Name) VALUES ('SystemAdmin');

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'Admin')
    INSERT INTO UserRole (Name) VALUES ('Admin');

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'OperatorAdmin')
    INSERT INTO UserRole (Name) VALUES ('OperatorAdmin');

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'Operator')
    INSERT INTO UserRole (Name) VALUES ('Operator');

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'Customer')
    INSERT INTO UserRole (Name) VALUES ('Customer');

IF NOT EXISTS (SELECT 1 FROM UserRole WHERE Name = 'Public')
    INSERT INTO UserRole (Name) VALUES ('Public');




-- Insert countries only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM Country WHERE Name = 'South Africa')
    INSERT INTO Country (Name) VALUES ('South Africa');

IF NOT EXISTS (SELECT 1 FROM Country WHERE Name = 'Zimbabwe')
    INSERT INTO Country (Name) VALUES ('Zimbabwe');


-- Insert regions for Zimbabwe only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Matebeland North', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Matebeland South', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Masvingo', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Midlands', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

-- Remove old Mashonaland region if it exists
DELETE FROM Region 
WHERE Name = 'Mashonaland' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe');

-- Insert new Mashonaland regions for Zimbabwe only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Mashonaland Central', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Mashonaland East', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Mashonaland West', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Manicaland' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Manicaland', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));


-- Insert towns for Matebeland North in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Lupane' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Lupane', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Victoria Falls' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Victoria Falls', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Hwange' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Hwange', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Binga' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Binga', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Nkayi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Nkayi', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Tsholotsho' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Tsholotsho', (SELECT RegionId FROM Region WHERE Name = 'Matebeland North' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Matebeland South in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Gwanda' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Gwanda', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Beitbridge' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Beitbridge', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Plumtree' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Plumtree', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Esigodini' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Esigodini', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Maphisa' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Maphisa', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Filabusi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Filabusi', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'West Nicholson' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('West Nicholson', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Ndolwane' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Ndolwane', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Shangani' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Shangani', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Stanmore' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Stanmore', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Kezi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Kezi', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Figtree' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Figtree', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Manama' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Manama', (SELECT RegionId FROM Region WHERE Name = 'Matebeland South' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Masvingo in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Masvingo' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Masvingo', (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mashava' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mashava', (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Chatsworth' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Chatsworth', (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Sango' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Sango', (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Felixburg' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Felixburg', (SELECT RegionId FROM Region WHERE Name = 'Masvingo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Midlands in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Gweru' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Gweru', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Kwekwe' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Kwekwe', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Shurugwi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Shurugwi', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Zvishavane' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Zvishavane', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Gokwe' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Gokwe', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Redcliff' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Redcliff', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mberengwa' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mberengwa', (SELECT RegionId FROM Region WHERE Name = 'Midlands' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert Harare region for Zimbabwe only if it does not already exist
IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
    INSERT INTO Region (Name, CountryId) VALUES ('Harare', (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'));

-- Insert towns for Harare region in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Chitungwiza' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Chitungwiza', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Epworth' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Epworth', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Ruwa' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Ruwa', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare South' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare South', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare West' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare West', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare North' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare North', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare East' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare East', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Harare Central' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Harare Central', (SELECT RegionId FROM Region WHERE Name = 'Harare' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Mashonaland Central in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Bindura' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Bindura', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mount Darwin' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mount Darwin', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Guruve' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Guruve', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Muzarabani' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Muzarabani', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Shamva' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Shamva', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Centenary' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Centenary', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mazowe' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mazowe', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Rushinga' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Rushinga', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland Central' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Mashonaland East in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Marondera' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Marondera', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Murehwa' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Murehwa', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mutoko' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mutoko', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Mudzi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Mudzi', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Wedza' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Wedza', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Maramba' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Maramba', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland East' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));


-- Insert towns for Mashonaland West in Zimbabwe only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Chinhoyi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Chinhoyi', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Karoi' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Karoi', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Zvimba' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Zvimba', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Makonde' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Makonde', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));

IF NOT EXISTS (
    SELECT 1 FROM Town 
    WHERE Name = 'Hurungwe' 
      AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe'))
)
    INSERT INTO Town (Name, RegionId) 
    VALUES ('Hurungwe', (SELECT RegionId FROM Region WHERE Name = 'Mashonaland West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'Zimbabwe')));




-- Insert South African regions (provinces) only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Eastern Cape', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Free State', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Gauteng', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('KwaZulu-Natal', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Limpopo', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Mpumalanga', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Northern Cape', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('North West', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

IF NOT EXISTS (SELECT 1 FROM Region WHERE Name = 'Western Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa'))
    INSERT INTO Region (Name, CountryId) VALUES ('Western Cape', (SELECT CountryId FROM Country WHERE Name = 'South Africa'));

-- Insert towns for each South African region only if they do not already exist

-- Eastern Cape
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Port Elizabeth (Gqeberha)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Port Elizabeth (Gqeberha)', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'East London (Buffalo City)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('East London (Buffalo City)', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Mthatha' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Mthatha', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Grahamstown (Makhanda)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Grahamstown (Makhanda)', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Queenstown (Komani)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Queenstown (Komani)', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Uitenhage' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Uitenhage', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'King William’s Town' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('King William’s Town', (SELECT RegionId FROM Region WHERE Name = 'Eastern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Free State
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Welkom' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Welkom', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Sasolburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Sasolburg', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Bethlehem' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Bethlehem', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Kroonstad' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Kroonstad', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Harrismith' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Harrismith', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Parys' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Parys', (SELECT RegionId FROM Region WHERE Name = 'Free State' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Gauteng
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Johannesburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Johannesburg', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Pretoria (Tshwane)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Pretoria (Tshwane)', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Soweto' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Soweto', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Sandton' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Sandton', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Midrand' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Midrand', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Roodepoort' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Roodepoort', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Vanderbijlpark' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Vanderbijlpark', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Germiston' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Germiston', (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- KwaZulu-Natal
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Durban (eThekwini)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Durban (eThekwini)', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Richards Bay' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Richards Bay', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Newcastle' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Newcastle', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Ladysmith' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Ladysmith', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Empangeni' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Empangeni', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Pietermaritzburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Pietermaritzburg', (SELECT RegionId FROM Region WHERE Name = 'KwaZulu-Natal' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Limpopo
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Polokwane (Pietersburg)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Polokwane (Pietersburg)', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Thohoyandou' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Thohoyandou', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Makhado (Louis Trichardt)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Makhado (Louis Trichardt)', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Bela-Bela (Warmbaths)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Bela-Bela (Warmbaths)', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Lephalale (Ellisras)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Lephalale (Ellisras)', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Groblersdal' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Groblersdal', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Tzaneen' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Tzaneen', (SELECT RegionId FROM Region WHERE Name = 'Limpopo' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Mpumalanga
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Mbombela (Nelspruit)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Mbombela (Nelspruit)', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Witbank (Emalahleni)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Witbank (Emalahleni)', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Middelburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Middelburg', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'White River' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('White River', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Lydenburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Lydenburg', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Sabie' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Sabie', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Barberton' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Barberton', (SELECT RegionId FROM Region WHERE Name = 'Mpumalanga' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Northern Cape
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Kimberley' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Kimberley', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Upington' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Upington', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Springbok' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Springbok', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Kuruman' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Kuruman', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'De Aar' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('De Aar', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Postmasburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Postmasburg', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Colesberg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Colesberg', (SELECT RegionId FROM Region WHERE Name = 'Northern Cape' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- North West
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Mahikeng (Mafikeng)' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Mahikeng (Mafikeng)', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Rustenburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Rustenburg', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Brits' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Brits', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Klerksdorp' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Klerksdorp', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Potchefstroom' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Potchefstroom', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Zeerust' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Zeerust', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));
IF NOT EXISTS (SELECT 1 FROM Town WHERE Name = 'Lichtenburg' AND RegionId = (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')))
    INSERT INTO Town (Name, RegionId) VALUES ('Lichtenburg', (SELECT RegionId FROM Region WHERE Name = 'North West' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')));

-- Insert LocationTypes only if they do not already exist
IF NOT EXISTS (SELECT 1 FROM LocationType WHERE Name = 'Bus station')
    INSERT INTO LocationType (Name) VALUES ('Bus station');

IF NOT EXISTS (SELECT 1 FROM LocationType WHERE Name = 'Bus stop')
    INSERT INTO LocationType (Name) VALUES ('Bus stop');

-- Insert locations in Johannesburg only if they do not already exist
IF NOT EXISTS (
    SELECT 1 FROM Location 
    WHERE Name = 'Powerhouse'
      AND TownId = (SELECT TownId FROM Town WHERE Name = 'Johannesburg')
)
    INSERT INTO Location (Name, TownId, RegionId, CountryId, LocationTypeId)
    VALUES (
        'Powerhouse',
        (SELECT TownId FROM Town WHERE Name = 'Johannesburg'),
        (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')),
        (SELECT CountryId FROM Country WHERE Name = 'South Africa'),
        (SELECT LocationTypeId FROM LocationType WHERE Name = 'Bus station')
    );

IF NOT EXISTS (
    SELECT 1 FROM Location 
    WHERE Name = 'Park station'
      AND TownId = (SELECT TownId FROM Town WHERE Name = 'Johannesburg')
)
    INSERT INTO Location (Name, TownId, RegionId, CountryId, LocationTypeId)
    VALUES (
        'Park station',
        (SELECT TownId FROM Town WHERE Name = 'Johannesburg'),
        (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')),
        (SELECT CountryId FROM Country WHERE Name = 'South Africa'),
        (SELECT LocationTypeId FROM LocationType WHERE Name = 'Bus station')
    );

-- Insert "Booysens Bus Stop" in Pretoria only if it does not already exist
IF NOT EXISTS (
    SELECT 1 FROM Location 
    WHERE Name = 'Booysens Bus Stop'
      AND TownId = (SELECT TownId FROM Town WHERE Name = 'Pretoria (Tshwane)')
)
    INSERT INTO Location (Name, TownId, RegionId, CountryId, LocationTypeId)
    VALUES (
        'Booysens Bus Stop',
        (SELECT TownId FROM Town WHERE Name = 'Pretoria (Tshwane)'),
        (SELECT RegionId FROM Region WHERE Name = 'Gauteng' AND CountryId = (SELECT CountryId FROM Country WHERE Name = 'South Africa')),
        (SELECT CountryId FROM Country WHERE Name = 'South Africa'),
        (SELECT LocationTypeId FROM LocationType WHERE Name = 'Bus stop')
    );