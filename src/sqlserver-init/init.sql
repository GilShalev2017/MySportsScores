-- Create a test database
CREATE DATABASE Sports365;
GO

-- Create a SQL login for Docker
CREATE LOGIN dockeruser WITH PASSWORD = 'Strong!Passw0rd';
GO

-- Create a user in the database
USE Sports365;
CREATE USER dockeruser FOR LOGIN dockeruser;
GO

-- Grant db_owner role to the user
ALTER ROLE db_owner ADD MEMBER dockeruser;
GO
