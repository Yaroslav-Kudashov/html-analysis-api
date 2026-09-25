using Microsoft.AspNetCore.Mvc;

namespace AnalysisApi;


/// <summary>
/// Логику try catch вынес бы в сервис, но т.к нужно компактно сделал так
/// </summary>
/// <param name="analysisService"></param>
[ApiController]
[Route("api")]
public sealed class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    [HttpPost("analysis")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await analysisService.ProcessAsync(request, cancellationToken));
        }
        catch (AnalysisException ex)
        {
            return Ok(new AnalysisResponse { IsError = 1, ErrorCode = ex.ErrorCode, ErrorMessage = ex.Message });
        }
        catch (Exception ex)
        {
            return Ok(new AnalysisResponse { IsError = 1, ErrorCode = ErrorCodes.InternalError, ErrorMessage = ex.Message });
        }
    }
}
