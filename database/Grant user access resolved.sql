-- HambaApp - this account is the alias for Managed Identity ie (account from External Provider) 


-- Create a database user mapped to the managed identity
    --CREATE USER [HambaApp] FROM EXTERNAL PROVIDER;
    --GO

-- Any DB permissions needed, use it


GRANT EXECUTE TO [HambaApp];
GO
