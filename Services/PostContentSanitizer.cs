using Ganss.Xss;

namespace AspNetBbs.Services;

public sealed class PostContentSanitizer
{
    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    public string Sanitize(string? content) => sanitizer.Sanitize(content ?? string.Empty);

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(["a", "b", "blockquote", "br", "div", "em", "h2", "h3", "hr", "i", "img", "li", "ol", "p", "s", "span", "strong", "u", "ul"]);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["alt", "href", "src", "style", "title"]);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedCssProperties.UnionWith(["color", "font-style", "font-weight", "text-decoration"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        return sanitizer;
    }
}
