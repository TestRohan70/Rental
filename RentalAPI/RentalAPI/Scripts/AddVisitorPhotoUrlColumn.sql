-- Add visitor photo path column for gate visitor image uploads
-- Run against Rental database after VisitorRequest table exists

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'VisitorRequest' AND COLUMN_NAME = 'VisitorPhotoUrl'
)
BEGIN
    ALTER TABLE VisitorRequest
    ADD VisitorPhotoUrl NVARCHAR(500) NULL;
END
GO
