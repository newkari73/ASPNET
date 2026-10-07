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

CREATE OR ALTER PROCEDURE [ExecWeb].[Member_SelectById]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[Member_Insert]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.MemberSocialLogin', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MemberSocialLogin]
    (
        ID INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_MemberSocialLogin PRIMARY KEY,
        UserID NVARCHAR(50) NOT NULL CONSTRAINT FK_MemberSocialLogin_Member FOREIGN KEY REFERENCES [dbo].[Member](UserID) ON DELETE CASCADE,
        Provider NVARCHAR(50) NOT NULL,
        ProviderKey NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        RegDate DATETIME NOT NULL CONSTRAINT DF_MemberSocialLogin_RegDate DEFAULT GETDATE(),
        CONSTRAINT UQ_MemberSocialLogin_Provider_ProviderKey UNIQUE (Provider, ProviderKey)
    );
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[MemberSocialLogin_SelectByProviderKey]
    @Provider NVARCHAR(50),
    @ProviderKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            M.UserID,
            M.UserName,
            SL.Email
        FROM [dbo].[MemberSocialLogin] AS SL
        INNER JOIN [dbo].[Member] AS M
            ON M.UserID = SL.UserID
        WHERE SL.Provider = @Provider
          AND SL.ProviderKey = @ProviderKey;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @Provider AS Provider,
                @ProviderKey AS ProviderKey
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[MemberSocialLogin_Insert]
    @UserID NVARCHAR(50),
    @Provider NVARCHAR(50),
    @ProviderKey NVARCHAR(100),
    @Email NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[MemberSocialLogin]
        (
            UserID,
            Provider,
            ProviderKey,
            Email,
            RegDate
        )
        VALUES
        (
            @UserID,
            @Provider,
            @ProviderKey,
            @Email,
            GETDATE()
        );
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @Provider AS Provider,
                @ProviderKey AS ProviderKey,
                @Email AS Email
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

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

CREATE OR ALTER PROCEDURE [ExecWeb].[Schedule_SelectByMonth]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[Schedule_Insert]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[Schedule_Delete]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

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

IF OBJECT_ID(N'dbo.PhotoBBS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhotoBBS
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhotoBBS PRIMARY KEY,
        UserID NVARCHAR(50) NULL,
        UserName NVARCHAR(50) NULL,
        Title NVARCHAR(200) NOT NULL,
        Contents NVARCHAR(MAX) NOT NULL,
        [File] NVARCHAR(500) NULL,
        RegDate DATETIME NOT NULL
            CONSTRAINT DF_PhotoBBS_RegDate DEFAULT GETDATE()
    );
END;
GO

IF COL_LENGTH(N'dbo.PhotoBBS', N'ID') IS NULL
BEGIN
    ALTER TABLE dbo.PhotoBBS ADD ID INT IDENTITY(1,1) NOT NULL;
END;
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PhotoBBS') AND type = 'PK')
BEGIN
    ALTER TABLE dbo.PhotoBBS ADD CONSTRAINT PK_PhotoBBS PRIMARY KEY CLUSTERED (ID);
END;
GO

IF COL_LENGTH(N'dbo.PhotoBBS', N'UserName') IS NULL
BEGIN
    ALTER TABLE dbo.PhotoBBS ADD UserName NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.PhotoBBS', N'UserID') IS NULL
BEGIN
    ALTER TABLE dbo.PhotoBBS ADD UserID NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.PhotoBBS SET UserName = N'관리자' WHERE UserName IS NULL;
GO

IF OBJECT_ID(N'dbo.PhotoBBSComment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhotoBBSComment
    (
        ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhotoBBSComment PRIMARY KEY,
        BbsID INT NOT NULL,
        UserID NVARCHAR(50) NULL,
        UserName NVARCHAR(50) NOT NULL,
        Contents NVARCHAR(1000) NOT NULL,
        RegDate DATETIME NOT NULL CONSTRAINT DF_PhotoBBSComment_RegDate DEFAULT GETDATE(),
        CONSTRAINT FK_PhotoBBSComment_PhotoBBS FOREIGN KEY (BbsID) REFERENCES dbo.PhotoBBS(ID) ON DELETE CASCADE
    );
END;
GO

IF COL_LENGTH(N'dbo.PhotoBBSComment', N'UserID') IS NULL
BEGIN
    ALTER TABLE dbo.PhotoBBSComment ADD UserID NVARCHAR(50) NULL;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBS_SelectAll]
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
        FROM [dbo].[PhotoBBS];

        SELECT
            PhotoBBS.ID,
            PhotoBBS.UserID,
            PhotoBBS.UserName,
            PhotoBBS.Title,
            PhotoBBS.Contents,
            PhotoBBS.[File],
            PhotoBBS.RegDate,
            COUNT(PhotoBBSComment.ID) AS CommentCount
        FROM [dbo].[PhotoBBS]
        LEFT JOIN [dbo].[PhotoBBSComment]
            ON PhotoBBSComment.BbsID = PhotoBBS.ID
        GROUP BY PhotoBBS.ID, PhotoBBS.UserID, PhotoBBS.UserName, PhotoBBS.Title, PhotoBBS.Contents, PhotoBBS.[File], PhotoBBS.RegDate
        ORDER BY PhotoBBS.ID DESC
        OFFSET (@Page - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @Page AS Page, @PageSize AS PageSize
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBS_SelectById]
    @ID INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            PhotoBBS.ID,
            PhotoBBS.UserID,
            PhotoBBS.UserName,
            PhotoBBS.Title,
            PhotoBBS.Contents,
            PhotoBBS.[File],
            PhotoBBS.RegDate,
            COUNT(PhotoBBSComment.ID) AS CommentCount
        FROM [dbo].[PhotoBBS]
        LEFT JOIN [dbo].[PhotoBBSComment]
            ON PhotoBBSComment.BbsID = PhotoBBS.ID
        WHERE PhotoBBS.ID = @ID
        GROUP BY PhotoBBS.ID, PhotoBBS.UserID, PhotoBBS.UserName, PhotoBBS.Title, PhotoBBS.Contents, PhotoBBS.[File], PhotoBBS.RegDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @ID AS ID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBS_Insert]
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
        INSERT INTO [dbo].[PhotoBBS]
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
        SET @ErrorInputValue = (
            SELECT @UserID AS UserID, @UserName AS UserName, @Title AS Title, @Contents AS Contents, @File AS [File], @RegDate AS RegDate
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBS_Update]
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
        UPDATE [dbo].[PhotoBBS]
        SET
            UserName = @UserName,
            Title = @Title,
            Contents = @Contents,
            [File] = @File
        WHERE ID = @ID AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @ID AS ID, @UserID AS UserID, @UserName AS UserName, @Title AS Title, @Contents AS Contents, @File AS [File]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBS_Delete]
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[PhotoBBS]
        WHERE ID = @ID AND UserID = @UserID;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @ID AS ID, @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBSComment_SelectByBbsId]
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
        FROM [dbo].[PhotoBBSComment]
        WHERE BbsID = @BbsID
        ORDER BY ID ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @BbsID AS BbsID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBSComment_Insert]
    @BbsID INT,
    @UserID NVARCHAR(50),
    @UserName NVARCHAR(50),
    @Contents NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[PhotoBBSComment]
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
        SET @ErrorInputValue = (
            SELECT @BbsID AS BbsID, @UserID AS UserID, @UserName AS UserName, @Contents AS Contents
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[PhotoBBSComment_Delete]
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[PhotoBBSComment]
        WHERE ID = @ID
          AND (UserID = @UserID OR @UserID IS NULL);
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);
        SET @ErrorInputValue = (
            SELECT @ID AS ID, @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;
        RETURN 0;
    END CATCH;
END;
GO

IF COL_LENGTH(N'dbo.BBSComment', N'UserID') IS NULL
BEGIN
    ALTER TABLE dbo.BBSComment ADD UserID NVARCHAR(50) NULL;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBS_SelectAll]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBS_SelectById]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBS_Insert]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBS_Update]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBS_Delete]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBSComment_SelectByBbsId]
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
        ORDER BY ID DESC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @BbsID AS BbsID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBSComment_Insert]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[BBSComment_Delete]
    @ID INT,
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DELETE FROM [dbo].[BBSComment]
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE INDEX IX_BBS_RegDate ON dbo.BBS (RegDate DESC);
GO-- 1. Member 테이블에 IsAdmin 컬럼 추가
IF COL_LENGTH(N'dbo.Member', N'IsAdmin') IS NULL
BEGIN
    ALTER TABLE [dbo].[Member] ADD IsAdmin BIT NOT NULL CONSTRAINT DF_Member_IsAdmin DEFAULT 0;
END;
GO

-- kari73을 관리자로 설정
UPDATE [dbo].[Member]
SET IsAdmin = 1
WHERE UserID = N'kari73';
GO

-- 2. Menu 테이블 생성
IF OBJECT_ID(N'dbo.Menu', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Menu]
    (
        MenuCode NVARCHAR(50) NOT NULL CONSTRAINT PK_Menu PRIMARY KEY,
        MenuName NVARCHAR(100) NOT NULL,
        ControllerName NVARCHAR(50) NOT NULL,
        ActionName NVARCHAR(50) NOT NULL CONSTRAINT DF_Menu_ActionName DEFAULT N'Index',
        SortOrder INT NOT NULL CONSTRAINT DF_Menu_SortOrder DEFAULT 1,
        IsActive BIT NOT NULL CONSTRAINT DF_Menu_IsActive DEFAULT 1
    );
END;
GO

-- 초기 메뉴 데이터 등록
IF NOT EXISTS (SELECT 1 FROM [dbo].[Menu] WHERE MenuCode = N'BBS')
    INSERT INTO [dbo].[Menu] (MenuCode, MenuName, ControllerName, ActionName, SortOrder, IsActive)
    VALUES (N'BBS', N'게시판', N'Bbs', N'Index', 1, 1);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menu] WHERE MenuCode = N'SCHEDULE')
    INSERT INTO [dbo].[Menu] (MenuCode, MenuName, ControllerName, ActionName, SortOrder, IsActive)
    VALUES (N'SCHEDULE', N'일정관리', N'Schedule', N'Index', 2, 1);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menu] WHERE MenuCode = N'MONITORING')
    INSERT INTO [dbo].[Menu] (MenuCode, MenuName, ControllerName, ActionName, SortOrder, IsActive)
    VALUES (N'MONITORING', N'모니터링', N'Monitoring', N'Index', 3, 1);
GO

-- 3. MemberMenuPermission 테이블 생성
IF OBJECT_ID(N'dbo.MemberMenuPermission', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MemberMenuPermission]
    (
        ID INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_MemberMenuPermission PRIMARY KEY,
        UserID NVARCHAR(50) NOT NULL CONSTRAINT FK_MemberMenuPermission_Member FOREIGN KEY REFERENCES [dbo].[Member](UserID) ON DELETE CASCADE,
        MenuCode NVARCHAR(50) NOT NULL CONSTRAINT FK_MemberMenuPermission_Menu FOREIGN KEY REFERENCES [dbo].[Menu](MenuCode) ON DELETE CASCADE,
        CanRead BIT NOT NULL CONSTRAINT DF_MemberMenuPermission_CanRead DEFAULT 1,
        CanUpdate BIT NOT NULL CONSTRAINT DF_MemberMenuPermission_CanUpdate DEFAULT 1,
        CanDelete BIT NOT NULL CONSTRAINT DF_MemberMenuPermission_CanDelete DEFAULT 1,
        RegDate DATETIME NOT NULL CONSTRAINT DF_MemberMenuPermission_RegDate DEFAULT GETDATE(),
        UpdateDate DATETIME NULL,
        CONSTRAINT UQ_MemberMenuPermission_User_Menu UNIQUE (UserID, MenuCode)
    );
END;
GO

-- 기존 사용자들에게 기본 권한 부여 (아직 없는 경우)
INSERT INTO [dbo].[MemberMenuPermission] (UserID, MenuCode, CanRead, CanUpdate, CanDelete)
SELECT M.UserID, MN.MenuCode, 1, 1, 1
FROM [dbo].[Member] AS M
CROSS JOIN [dbo].[Menu] AS MN
LEFT JOIN [dbo].[MemberMenuPermission] AS P
    ON P.UserID = M.UserID AND P.MenuCode = MN.MenuCode
WHERE P.ID IS NULL;
GO

-- 4. 저장 프로시저 정의

CREATE OR ALTER PROCEDURE [ExecWeb].[Menu_SelectAll]
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            MenuCode,
            MenuName,
            ControllerName,
            ActionName,
            SortOrder,
            IsActive
        FROM [dbo].[Menu]
        WHERE IsActive = 1
        ORDER BY SortOrder ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue = N'{}';

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[MemberMenuPermission_SelectByUserId]
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            M.MenuCode,
            M.MenuName,
            M.ControllerName,
            M.ActionName,
            M.SortOrder,
            CONVERT(BIT, ISNULL(P.CanRead, 1)) AS CanRead,
            CONVERT(BIT, ISNULL(P.CanUpdate, 1)) AS CanUpdate,
            CONVERT(BIT, ISNULL(P.CanDelete, 1)) AS CanDelete
        FROM [dbo].[Menu] AS M
        CROSS JOIN [dbo].[Member] AS MB
        LEFT JOIN [dbo].[MemberMenuPermission] AS P
            ON P.MenuCode = M.MenuCode AND P.UserID = MB.UserID
        WHERE MB.UserID = @UserID
          AND M.IsActive = 1
        ORDER BY M.SortOrder ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[MemberMenuPermission_Save]
    @UserID NVARCHAR(50),
    @MenuCode NVARCHAR(50),
    @CanRead BIT,
    @CanUpdate BIT,
    @CanDelete BIT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM [dbo].[MemberMenuPermission] WHERE UserID = @UserID AND MenuCode = @MenuCode)
        BEGIN
            UPDATE [dbo].[MemberMenuPermission]
            SET
                CanRead = @CanRead,
                CanUpdate = @CanUpdate,
                CanDelete = @CanDelete,
                UpdateDate = GETDATE()
            WHERE UserID = @UserID
              AND MenuCode = @MenuCode;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[MemberMenuPermission]
            (
                UserID,
                MenuCode,
                CanRead,
                CanUpdate,
                CanDelete,
                RegDate
            )
            VALUES
            (
                @UserID,
                @MenuCode,
                @CanRead,
                @CanUpdate,
                @CanDelete,
                GETDATE()
            );
        END
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue =
        (
            SELECT
                @UserID AS UserID,
                @MenuCode AS MenuCode,
                @CanRead AS CanRead,
                @CanUpdate AS CanUpdate,
                @CanDelete AS CanDelete
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[Member_SelectAllWithPermissions]
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            MB.UserID,
            MB.UserName,
            MB.IsAdmin,
            M.MenuCode,
            M.MenuName,
            CONVERT(BIT, ISNULL(P.CanRead, 1)) AS CanRead,
            CONVERT(BIT, ISNULL(P.CanUpdate, 1)) AS CanUpdate,
            CONVERT(BIT, ISNULL(P.CanDelete, 1)) AS CanDelete
        FROM [dbo].[Member] AS MB
        CROSS JOIN [dbo].[Menu] AS M
        LEFT JOIN [dbo].[MemberMenuPermission] AS P
            ON P.UserID = MB.UserID AND P.MenuCode = M.MenuCode
        WHERE M.IsActive = 1
        ORDER BY MB.IsAdmin DESC, MB.UserID ASC, M.SortOrder ASC;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorInputValue NVARCHAR(MAX);

        SET @ErrorInputValue = N'{}';

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [ExecWeb].[Member_CheckIsAdmin]
    @UserID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            ISNULL(IsAdmin, 0) AS IsAdmin
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

        EXEC [ExecWeb].[c_RaiseError] @ErrorInputValue;

        RETURN 0;
    END CATCH;
END;
GO
