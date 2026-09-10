USE [ASPNET];
GO

IF OBJECT_ID(N'dbo.Member', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Member]
    (
        UserID NVARCHAR(50) NOT NULL CONSTRAINT PK_Member PRIMARY KEY,
        UserName NVARCHAR(50) NOT NULL,
        UserPWD NVARCHAR(500) NOT NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[Member_SelectById]
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            UserID,
            UserName,
            UserPWD
        FROM [dbo].[Member]
        WHERE UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[Member_Insert]
    @UserID NVARCHAR(50),
    @UserName NVARCHAR(50),
    @UserPWD NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[Member]
        (
            UserID,
            UserName,
            UserPWD
        )
        VALUES
        (
            @UserID,
            @UserName,
            @UserPWD
        );
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @UserName AS UserName,
                @UserPWD AS UserPWD
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.Schedule', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Schedule]
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Schedule PRIMARY KEY,
        UserID NVARCHAR(50) NOT NULL,
        ScheduleDate DATE NULL,
        StartDateTime DATETIME2 NOT NULL,
        EndDateTime DATETIME2 NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Contents NVARCHAR(2000) NULL,
        RegDate DATETIME NOT NULL CONSTRAINT DF_Schedule_RegDate DEFAULT GETDATE(),
        CONSTRAINT FK_Schedule_Member FOREIGN KEY (UserID) REFERENCES [dbo].[Member](UserID) ON DELETE CASCADE
    );
END;
GO

IF COL_LENGTH(N'dbo.Schedule', N'StartDateTime') IS NULL
BEGIN
    ALTER TABLE [dbo].[Schedule] ADD StartDateTime DATETIME2 NULL;
    ALTER TABLE [dbo].[Schedule] ADD EndDateTime DATETIME2 NULL;
END;
GO

UPDATE [dbo].[Schedule]
SET
    StartDateTime = COALESCE(StartDateTime, CONVERT(DATETIME2, ScheduleDate)),
    EndDateTime = COALESCE(EndDateTime, DATEADD(HOUR, 1, CONVERT(DATETIME2, ScheduleDate)))
WHERE StartDateTime IS NULL OR EndDateTime IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Schedule]') AND name = N'StartDateTime' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[Schedule] ALTER COLUMN StartDateTime DATETIME2 NOT NULL;
    ALTER TABLE [dbo].[Schedule] ALTER COLUMN EndDateTime DATETIME2 NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[Schedule_SelectByMonth]
    @UserID NVARCHAR(50),
    @Month DATE
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            ID,
            UserID,
            StartDateTime,
            EndDateTime,
            Title,
            Contents,
            RegDate
        FROM [dbo].[Schedule]
                WHERE UserID = @UserID
                    AND EndDateTime >= DATEFROMPARTS(YEAR(@Month), MONTH(@Month), 1)
                    AND StartDateTime < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(@Month), MONTH(@Month), 1))
                ORDER BY StartDateTime ASC, ID ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @Month AS Month
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[Schedule_Insert]
    @UserID NVARCHAR(50),
    @StartDateTime DATETIME2,
    @EndDateTime DATETIME2,
    @Title NVARCHAR(200),
    @Contents NVARCHAR(2000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[Schedule]
        (
            UserID,
            ScheduleDate,
            StartDateTime,
            EndDateTime,
            Title,
            Contents
        )
        VALUES
        (
            @UserID,
            CONVERT(DATE, @StartDateTime),
            @StartDateTime,
            @EndDateTime,
            @Title,
            @Contents
        );
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @StartDateTime AS StartDateTime,
                @EndDateTime AS EndDateTime,
                @Title AS Title,
                @Contents AS Contents
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[Schedule_Delete]
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[Schedule]
        WHERE ID = @ID
          AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @ID AS ID,
                @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.BBS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BBS
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BBS PRIMARY KEY,
        UserID NVARCHAR(50) NULL,
        Title NVARCHAR(200) NOT NULL,
        Contents NVARCHAR(MAX) NOT NULL,
        [File] NVARCHAR(500) NULL,
        RegDate DATETIME NOT NULL
            CONSTRAINT DF_BBS_RegDate DEFAULT GETDATE()
    );
END;
GO

IF COL_LENGTH(N'dbo.BBS', N'ID') IS NULL
BEGIN
    ALTER TABLE dbo.BBS ADD ID INT IDENTITY(1,1) NOT NULL;
END;
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.BBS') AND type = 'PK')
BEGIN
    ALTER TABLE dbo.BBS ADD CONSTRAINT PK_BBS PRIMARY KEY CLUSTERED (ID);
END;
GO

IF COL_LENGTH(N'dbo.BBS', N'UserName') IS NULL
BEGIN
    ALTER TABLE dbo.BBS ADD UserName NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.BBS', N'UserID') IS NULL
BEGIN
    ALTER TABLE dbo.BBS ADD UserID NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.BBS SET UserName = N'관리자' WHERE UserName IS NULL;
GO

IF OBJECT_ID(N'dbo.BBSComment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BBSComment
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BBSComment PRIMARY KEY,
        BbsID INT NOT NULL,
        UserID NVARCHAR(50) NULL,
        UserName NVARCHAR(50) NOT NULL,
        Contents NVARCHAR(1000) NOT NULL,
        RegDate DATETIME NOT NULL CONSTRAINT DF_BBSComment_RegDate DEFAULT GETDATE(),
        CONSTRAINT FK_BBSComment_BBS FOREIGN KEY (BbsID) REFERENCES dbo.BBS(ID) ON DELETE CASCADE
    );
END;
GO

IF COL_LENGTH(N'dbo.BBSComment', N'UserID') IS NULL
BEGIN
    ALTER TABLE dbo.BBSComment ADD UserID NVARCHAR(50) NULL;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBS_SelectAll]
    @Page INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SET @Page = CASE
                        WHEN @Page < 1 THEN 1
                        ELSE @Page
                    END;

        SET @PageSize = CASE
                            WHEN @PageSize < 1 OR @PageSize > 100 THEN 10
                            ELSE @PageSize
                        END;

        SELECT COUNT(*) AS TotalCount
        FROM [dbo].[BBS];

        SELECT
            BBS.ID,
            BBS.UserID,
            BBS.UserName,
            BBS.Title,
            BBS.Contents,
            BBS.[File],
            BBS.RegDate,
            COUNT(BBSComment.ID) AS CommentCount
        FROM [dbo].[BBS]
        LEFT JOIN [dbo].[BBSComment]
            ON BBSComment.BbsID = BBS.ID
        GROUP BY BBS.ID, BBS.UserID, BBS.UserName, BBS.Title, BBS.Contents, BBS.[File], BBS.RegDate
        ORDER BY BBS.ID DESC
        OFFSET (@Page - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @Page AS Page,
                @PageSize AS PageSize
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBS_SelectById]
    @ID INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            BBS.ID,
            BBS.UserID,
            BBS.UserName,
            BBS.Title,
            BBS.Contents,
            BBS.[File],
            BBS.RegDate,
            COUNT(BBSComment.ID) AS CommentCount
        FROM [dbo].[BBS]
        LEFT JOIN [dbo].[BBSComment]
            ON BBSComment.BbsID = BBS.ID
        WHERE BBS.ID = @ID
        GROUP BY BBS.ID, BBS.UserID, BBS.UserName, BBS.Title, BBS.Contents, BBS.[File], BBS.RegDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @ID AS ID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBS_Insert]
    @UserID NVARCHAR(50),
    @UserName NVARCHAR(50),
    @Title NVARCHAR(200),
    @Contents NVARCHAR(MAX),
    @File NVARCHAR(500) = NULL,
    @RegDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[BBS]
        (
            UserID,
            UserName,
            Title,
            Contents,
            [File],
            RegDate
        )
        VALUES
        (
            @UserID,
            @UserName,
            @Title,
            @Contents,
            @File,
            @RegDate
        );

        SELECT CONVERT(INT, SCOPE_IDENTITY()) AS ID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @UserName AS UserName,
                @Title AS Title,
                @Contents AS Contents,
                @File AS [File],
                @RegDate AS RegDate
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBS_Update]
    @ID INT,
    @UserID NVARCHAR(50),
    @UserName NVARCHAR(50),
    @Title NVARCHAR(200),
    @Contents NVARCHAR(MAX),
    @File NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE [dbo].[BBS]
        SET
            UserName = @UserName,
            Title = @Title,
            Contents = @Contents,
            [File] = @File
                WHERE ID = @ID
                    AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @ID AS ID,
                @UserID AS UserID,
                @UserName AS UserName,
                @Title AS Title,
                @Contents AS Contents,
                @File AS [File]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBS_Delete]
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[BBS]
                WHERE ID = @ID
                    AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @ID AS ID,
                @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBSComment_SelectByBbsId]
    @BbsID INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            ID,
            BbsID,
            UserID,
            UserName,
            Contents,
            RegDate
        FROM [dbo].[BBSComment]
        WHERE BbsID = @BbsID
        ORDER BY ID ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @BbsID AS BbsID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBSComment_Insert]
    @BbsID INT,
    @UserID NVARCHAR(50),
    @UserName NVARCHAR(50),
    @Contents NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[BBSComment]
        (
            BbsID,
            UserID,
            UserName,
            Contents,
            RegDate
        )
        VALUES
        (
            @BbsID,
            @UserID,
            @UserName,
            @Contents,
            GETDATE()
        );
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @BbsID AS BbsID,
                @UserID AS UserID,
                @UserName AS UserName,
                @Contents AS Contents
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[BBSComment_Delete]
    @ID INT
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[BBSComment]
        WHERE ID = @ID;
        WHERE ID = @ID
          AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @ID AS ID
                @ID AS ID,
                @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [dbo].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE INDEX IX_BBS_RegDate ON dbo.BBS (RegDate DESC);
GO