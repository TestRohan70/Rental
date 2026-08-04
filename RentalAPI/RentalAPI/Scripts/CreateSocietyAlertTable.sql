IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SocietyAlert')
BEGIN
    CREATE TABLE SocietyAlert (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        AlertType NVARCHAR(50) NOT NULL DEFAULT 'General',
        CreatedBySecurityId INT NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_SocietyAlert_Security FOREIGN KEY (CreatedBySecurityId) REFERENCES Resident(Id)
    );
END
