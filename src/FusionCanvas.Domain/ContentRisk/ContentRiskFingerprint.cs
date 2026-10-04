using System.Security.Cryptography;
using System.Text;

namespace FusionCanvas.Domain.ContentRisk;

public static class ContentRiskFingerprint
{
    public static string ForText(ContentRiskReviewTarget target, string text)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        if (target.ContentKind != ContentRiskContentKind.Text)
        {
            throw new ArgumentException("The target must represent text content.", nameof(target));
        }

        return Hash(target, NormalizeText(text));
    }

    public static string ForImage(ContentRiskReviewTarget target, ReadOnlySpan<byte> imageBytes)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.ContentKind != ContentRiskContentKind.Image)
        {
            throw new ArgumentException("The target must represent image content.", nameof(target));
        }

        var roleBytes = Encoding.UTF8.GetBytes(target.Role);
        var separator = new byte[] { 0 };
        var input = new byte[roleBytes.Length + separator.Length + imageBytes.Length];
        roleBytes.CopyTo(input, 0);
        separator.CopyTo(input, roleBytes.Length);
        imageBytes.CopyTo(input.AsSpan(roleBytes.Length + separator.Length));
        return Convert.ToHexString(SHA256.HashData(input));
    }

    public static string NormalizeText(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string Hash(ContentRiskReviewTarget target, string normalizedText)
    {
        var input = Encoding.UTF8.GetBytes($"{target.Role}\0{normalizedText}");
        return Convert.ToHexString(SHA256.HashData(input));
    }
}
