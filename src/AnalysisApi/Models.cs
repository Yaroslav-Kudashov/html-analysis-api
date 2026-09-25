namespace AnalysisApi;

/// <summary>
/// Модели вынес бы по разным классам, и вместо класса ErrorCodes создал бы файлик с кодами ошибок и их значение 
/// </summary>

public sealed class AnalysisRequest
{
    public string? Selector { get; set; }

    public string? Attribute { get; set; }

    public string? UrlB64 { get; set; }

    public string? EncryptedTextBytesB64 { get; set; }

    public string? KeyBytesB64 { get; set; }

    public string? PageB64 { get; set; }
}

public sealed class AnalysisResponse
{
    public int IsError { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int ElementsCount { get; set; }

    public int EmailsCount { get; set; }

    public string? Url { get; set; }

    public string? DecryptedPlainText { get; set; }

    public List<string> ElementsAttrList { get; set; } = [];

    public List<string> EmailsList { get; set; } = [];
}

public sealed class AnalysisException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed record ElementRow(string AttrValue, string ElementHtml);

public static class ErrorCodes
{
    public const string MissingParameter = "MISSING_PARAMETER";

    public const string EmptySelector = "EMPTY_SELECTOR";

    public const string EmptyAttribute = "EMPTY_ATTRIBUTE";

    public const string InvalidUrlBase64 = "INVALID_URL_BASE64";

    public const string InvalidPageBase64 = "INVALID_PAGE_BASE64";

    public const string InvalidKeyBase64 = "INVALID_KEY_BASE64";

    public const string InvalidCipherBase64 = "INVALID_CIPHER_BASE64";

    public const string InvalidKeySize = "INVALID_KEY_SIZE";

    public const string InvalidSelector = "INVALID_SELECTOR";

    public const string DecryptionFailed = "DECRYPTION_FAILED";

    public const string DbError = "DB_ERROR";

    public const string InvalidJson = "INVALID_JSON";

    public const string InternalError = "INTERNAL_ERROR";
}
