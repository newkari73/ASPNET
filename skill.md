# SQL Stored Procedure Style

## Sample: [dbo].[BBS_SelectAll]

```sql
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
            BBS.UserName,
            BBS.Title,
            BBS.Contents,
            BBS.[File],
            BBS.RegDate,
            COUNT(BBSComment.ID) AS CommentCount
        FROM [dbo].[BBS]
        LEFT JOIN [dbo].[BBSComment]
            ON BBSComment.BbsID = BBS.ID
        GROUP BY BBS.ID, BBS.UserName, BBS.Title, BBS.Contents, BBS.[File], BBS.RegDate
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
```

## Rules

- Always use the two-part name `[dbo].[ProcedureName]`.
- Put each parameter on its own indented line.
- Use `SET NOCOUNT ON;` immediately inside the procedure body.
- Put each selected column on its own line.
- Qualify table names with `[dbo]`.
- Use explicit `BEGIN` and `END` blocks.
- Wrap executable statements in `BEGIN TRY` and `BEGIN CATCH` blocks.
- Save input parameters as JSON in `@ErrorInputValue` inside `BEGIN CATCH`.
- Call `[dbo].[c_RaiseError] @ErrorInputValue` inside `BEGIN CATCH`.
- End the Catch block with `RETURN 0`.
- Separate statements with blank lines when they represent separate operations.
- End each procedure definition with `GO`.
