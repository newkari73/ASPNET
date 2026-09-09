using AspNetBbs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AspNetBbs.Controllers;

public class BbsController(IConfiguration configuration, IWebHostEnvironment environment) : Controller
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection이 설정되지 않았습니다.");

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetString("UserID") is null)
        {
            context.Result = RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path });
            return;
        }

        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 10;
        page = Math.Max(page, 1);
        var posts = new List<BbsPost>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBS_SelectAll");
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        await using var reader = await command.ExecuteReaderAsync();

        var totalCount = await reader.ReadAsync() ? reader.GetInt32(0) : 0;
        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            posts.Add(MapPost(reader));
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            return RedirectToAction(nameof(Index), new { page = totalPages });
        }

        return View(new BbsPageViewModel
        {
            Posts = posts,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var post = await FindPostAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        return View(new BbsDetailViewModel
        {
            Post = post,
            Comments = await FindCommentsAsync(id)
        });
    }

    public IActionResult Create() => View(new BbsPost());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BbsPost post, IFormFile? upload)
    {
        post.UserName = CurrentUserName;
        post.UserID = CurrentUserId;
        ModelState.Remove(nameof(BbsPost.UserName));
        if (!ModelState.IsValid)
        {
            return View(post);
        }

        post.RegDate = DateTime.Now;
        post.File = await SaveFileAsync(upload);
        await ExecuteAsync("dbo.BBS_Insert", post, includeId: false);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var post = await FindPostAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        return post.UserID == CurrentUserId ? View(post) : Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BbsPost post, IFormFile? upload)
    {
        if (id != post.ID)
        {
            return BadRequest();
        }

        var existingPost = await FindPostAsync(id);
        if (existingPost?.UserID != CurrentUserId)
        {
            return Forbid();
        }

        post.UserName = CurrentUserName;
        post.UserID = CurrentUserId;
        ModelState.Remove(nameof(BbsPost.UserName));
        if (!ModelState.IsValid)
        {
            return View(post);
        }

        post.UserName = CurrentUserName;
        post.File = await SaveFileAsync(upload) ?? post.File;
        await ExecuteAsync("dbo.BBS_Update", post, includeId: true);
        return RedirectToAction(nameof(Details), new { id = post.ID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBS_Delete");
        command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = CurrentUserId;
        await command.ExecuteNonQueryAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int bbsId, BbsComment comment)
    {
        if (bbsId != comment.BbsID)
        {
            return BadRequest();
        }

        comment.UserName = CurrentUserName;
        comment.UserID = CurrentUserId;
        ModelState.Remove(nameof(BbsComment.UserName));
        ModelState.Remove(nameof(BbsComment.UserID));
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = bbsId });
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBSComment_Insert");
        command.Parameters.Add("@BbsID", SqlDbType.Int).Value = comment.BbsID;
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = comment.UserID;
        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = comment.UserName;
        command.Parameters.Add("@Contents", SqlDbType.NVarChar, 1000).Value = comment.Contents;
        await command.ExecuteNonQueryAsync();
        return RedirectToAction(nameof(Details), new { id = bbsId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int bbsId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBSComment_Delete");
        command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = CurrentUserId;
        await command.ExecuteNonQueryAsync();
        return RedirectToAction(nameof(Details), new { id = bbsId });
    }

    private async Task<BbsPost?> FindPostAsync(int id)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBS_SelectById");
        command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapPost(reader) : null;
    }

    private async Task ExecuteAsync(string procedureName, BbsPost post, bool includeId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, procedureName);
        if (includeId)
        {
            command.Parameters.Add("@ID", SqlDbType.Int).Value = post.ID;
        }
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = post.Title;
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = (object?)post.UserID ?? DBNull.Value;
        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = post.UserName;
        command.Parameters.Add("@Contents", SqlDbType.NVarChar, -1).Value = post.Contents;
        command.Parameters.Add("@File", SqlDbType.NVarChar, 500).Value = (object?)post.File ?? DBNull.Value;
        if (!includeId)
        {
            command.Parameters.Add("@RegDate", SqlDbType.DateTime).Value = post.RegDate;
        }
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> SaveFileAsync(IFormFile? upload)
    {
        if (upload is null || upload.Length == 0)
        {
            return null;
        }

        var uploads = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploads);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(upload.FileName)}";
        await using var stream = System.IO.File.Create(Path.Combine(uploads, storedName));
        await upload.CopyToAsync(stream);
        return storedName;
    }

    private static BbsPost MapPost(SqlDataReader reader) => new()
    {
        ID = reader.GetInt32(reader.GetOrdinal("ID")),
        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? null : reader.GetString(reader.GetOrdinal("UserID")),
        CommentCount = reader.GetInt32(reader.GetOrdinal("CommentCount")),
        UserName = reader.GetString(reader.GetOrdinal("UserName")),
        Title = reader.GetString(reader.GetOrdinal("Title")),
        Contents = reader.GetString(reader.GetOrdinal("Contents")),
        File = reader.IsDBNull(reader.GetOrdinal("File")) ? null : reader.GetString(reader.GetOrdinal("File")),
        RegDate = reader.GetDateTime(reader.GetOrdinal("RegDate"))
    };

    private async Task<List<BbsComment>> FindCommentsAsync(int bbsId)
    {
        var comments = new List<BbsComment>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, "dbo.BBSComment_SelectByBbsId");
        command.Parameters.Add("@BbsID", SqlDbType.Int).Value = bbsId;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            comments.Add(new BbsComment
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                BbsID = reader.GetInt32(reader.GetOrdinal("BbsID")),
                UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? null : reader.GetString(reader.GetOrdinal("UserID")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Contents = reader.GetString(reader.GetOrdinal("Contents")),
                RegDate = reader.GetDateTime(reader.GetOrdinal("RegDate"))
            });
        }

        return comments;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedureName) => new(procedureName, connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    private string CurrentUserId => HttpContext.Session.GetString("UserID")!;
    private string CurrentUserName => HttpContext.Session.GetString("UserName") ?? "회원";
}