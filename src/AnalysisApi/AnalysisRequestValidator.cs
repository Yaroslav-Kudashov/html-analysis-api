using FluentValidation;

namespace AnalysisApi;

public sealed class AnalysisRequestValidator : AbstractValidator<AnalysisRequest>
{
    public AnalysisRequestValidator()
    {
        RuleFor(x => x.Selector)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр selector")
            .NotEmpty().WithErrorCode(ErrorCodes.EmptySelector).WithMessage("Параметр selector пустой");

        RuleFor(x => x.Attribute)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр attribute")
            .NotEmpty().WithErrorCode(ErrorCodes.EmptyAttribute).WithMessage("Параметр attribute пустой");

        RuleFor(x => x.UrlB64)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр url_b64")
            .NotEmpty().WithErrorCode(ErrorCodes.InvalidUrlBase64).WithMessage("Параметр url_b64 пустой");

        RuleFor(x => x.EncryptedTextBytesB64)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр encrypted_text_bytes_b64")
            .NotEmpty().WithErrorCode(ErrorCodes.InvalidCipherBase64).WithMessage("Параметр encrypted_text_bytes_b64 пустой");

        RuleFor(x => x.KeyBytesB64)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр key_bytes_b64")
            .NotEmpty().WithErrorCode(ErrorCodes.InvalidKeyBase64).WithMessage("Параметр key_bytes_b64 пустой");

        RuleFor(x => x.PageB64)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithErrorCode(ErrorCodes.MissingParameter).WithMessage("Во входящем объекте отсутствует параметр page_b64")
            .NotEmpty().WithErrorCode(ErrorCodes.InvalidPageBase64).WithMessage("Параметр page_b64 пустой");
    }
}
