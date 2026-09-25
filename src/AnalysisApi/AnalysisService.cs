using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Dapper;
using FluentValidation;
using Npgsql;

namespace AnalysisApi;

/// <summary>
/// В сервисе много приваток, вынес бы в отдельные сервисы, но т.к просили сделать компактно сделал так
/// </summary>

public interface IAnalysisService
{
    Task<AnalysisResponse> ProcessAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

public sealed partial class AnalysisService(
    IValidator<AnalysisRequest> validator,
    NpgsqlDataSource dataSource) : IAnalysisService
{
    public async Task<AnalysisResponse> ProcessAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            throw new AnalysisException(failure.ErrorCode, failure.ErrorMessage);
        }

        var url = Encoding.UTF8.GetString(DecodeBase64(request.UrlB64!, ErrorCodes.InvalidUrlBase64));
        var pageHtml = Encoding.UTF8.GetString(DecodeBase64(request.PageB64!, ErrorCodes.InvalidPageBase64));
        var keyBytes = DecodeBase64(request.KeyBytesB64!, ErrorCodes.InvalidKeyBase64);
        var cipherBytes = DecodeBase64(request.EncryptedTextBytesB64!, ErrorCodes.InvalidCipherBase64);

        var elements = await SelectElementsAsync(pageHtml, request.Selector!, cancellationToken);

        var attrValues = new List<string>(elements.Count);
        var rows = new List<ElementRow>(elements.Count);
        foreach (var element in elements)
        {
            var value = element.GetAttribute(request.Attribute!) ?? string.Empty;
            attrValues.Add(value);
            rows.Add(new ElementRow(value, element.OuterHtml));
        }

        if (rows.Count > 0)
        {
            await SaveElementsAsync(rows, cancellationToken);
        }

        var emails = ExtractEmails(pageHtml);
        var decryptedText = await DecryptAsync(keyBytes, cipherBytes, cancellationToken);

        return new AnalysisResponse
        {
            IsError = 0,
            ElementsCount = attrValues.Count,
            EmailsCount = emails.Count,
            Url = url,
            DecryptedPlainText = decryptedText,
            ElementsAttrList = attrValues,
            EmailsList = emails,
        };
    }

    private static byte[] DecodeBase64(string value, string errorCode)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new AnalysisException(errorCode, ex.Message);
        }
    }

    private static async Task<List<IElement>> SelectElementsAsync(string pageHtml, string selector, CancellationToken cancellationToken)
    {
        using var browsingContext = BrowsingContext.New(Configuration.Default);
        var document = await browsingContext.OpenAsync(r => r.Content(pageHtml).Address("http://analysis.local/"), cancellationToken);

        try
        {
            return [.. document.QuerySelectorAll(selector)];
        }
        catch (Exception ex)
        {
            throw new AnalysisException(ErrorCodes.InvalidSelector, ex.Message);
        }
    }

    private async Task SaveElementsAsync(List<ElementRow> rows, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO elements (attr_value, element_html) VALUES (@AttrValue, @ElementHtml)",
                rows,
                cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            throw new AnalysisException(ErrorCodes.DbError, ex.Message);
        }
    }

    private static List<string> ExtractEmails(string pageHtml)
    {
        var emails = new List<string>();
        foreach (Match match in EmailRegex().Matches(pageHtml))
        {
            emails.Add(match.Value);
        }

        return emails;
    }

    private static async Task<string> DecryptAsync(byte[] keyBytes, byte[] cipherBytes, CancellationToken cancellationToken)
    {
        if (keyBytes.Length != 32)
        {
            throw new AnalysisException(ErrorCodes.InvalidKeySize,
                $"Ключ AES-256 должен быть длиной 32 байта, получено {keyBytes.Length}");
        }

        if (cipherBytes.Length == 0 || cipherBytes.Length % 16 != 0)
        {
            throw new AnalysisException(ErrorCodes.DecryptionFailed,
                $"Длина шифротекста должна быть кратна размеру блока AES (16 байт), получено {cipherBytes.Length}");
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            using var decryptor = aes.CreateDecryptor();
            using var cipherStream = new MemoryStream(cipherBytes, writable: false);
            using var cryptoStream = new CryptoStream(cipherStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream, Encoding.UTF8);

            return (await reader.ReadToEndAsync(cancellationToken)).TrimEnd('\0');
        }
        catch (Exception ex)
        {
            throw new AnalysisException(ErrorCodes.DecryptionFailed, ex.Message);
        }
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();
}
