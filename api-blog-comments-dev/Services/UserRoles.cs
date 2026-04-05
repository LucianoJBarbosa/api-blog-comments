namespace api_blog_comments_dev.Services;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Author = "Author";
    public const string AdminOrAuthor = Admin + "," + Author;
}