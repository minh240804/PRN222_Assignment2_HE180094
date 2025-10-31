-- Add DeletedBy and DeletedAt columns to Comment table
-- Run this script on your database

-- Check if columns don't exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Comment' AND COLUMN_NAME = 'DeletedBy')
BEGIN
    ALTER TABLE [dbo].[Comment]
    ADD [DeletedBy] SMALLINT NULL,
        [DeletedAt] DATETIME NULL;
END
GO

-- Add foreign key constraint for DeletedBy
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
               WHERE CONSTRAINT_NAME = 'FK_Comment_SystemAccount_DeletedBy')
BEGIN
    ALTER TABLE [dbo].[Comment]
    ADD CONSTRAINT [FK_Comment_SystemAccount_DeletedBy] 
    FOREIGN KEY ([DeletedBy])
    REFERENCES [dbo].[SystemAccount] ([AccountID])
    ON DELETE NO ACTION;
END
GO

-- Create index for better performance when querying deleted comments
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Comment_DeletedBy' 
               AND object_id = OBJECT_ID('Comment'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Comment_DeletedBy] 
    ON [dbo].[Comment] ([DeletedBy] ASC)
    WHERE [DeletedBy] IS NOT NULL;
END
GO

-- Verify the changes
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Comment'
AND COLUMN_NAME IN ('DeletedBy', 'DeletedAt');
GO
