using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("csrf")]
public class CrossForgeryController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet]
    [Route("token")]
    public ActionResult<string> answerCheck()
    {
        var tokenSet = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok( new {token = tokenSet.RequestToken});
    }
}