-- Visitor gate feature: run against Rental database
-- Creates VisitorRequest table for Security -> Resident approval flow

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'VisitorRequest'
)
BEGIN
    CREATE TABLE VisitorRequest (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VisitorName NVARCHAR(200) NOT NULL,
        VisitorPhone NVARCHAR(20) NULL,
        Purpose NVARCHAR(500) NULL,
        Wing NVARCHAR(50) NOT NULL,
        FlatNo INT NOT NULL,
        ResidentId INT NOT NULL,
        SecurityId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_VisitorRequest_Status DEFAULT ('Pending'),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_VisitorRequest_CreatedDate DEFAULT (GETUTCDATE()),
        RespondedDate DATETIME NULL,
        AcknowledgedDate DATETIME NULL,
        CONSTRAINT FK_VisitorRequest_Resident FOREIGN KEY (ResidentId) REFERENCES Resident(Id),
        CONSTRAINT FK_VisitorRequest_Security FOREIGN KEY (SecurityId) REFERENCES Resident(Id)
    );

    CREATE INDEX IX_VisitorRequest_SecurityId ON VisitorRequest(SecurityId);
    CREATE INDEX IX_VisitorRequest_ResidentId ON VisitorRequest(ResidentId);
    CREATE INDEX IX_VisitorRequest_Status ON VisitorRequest(Status);
END
GO
