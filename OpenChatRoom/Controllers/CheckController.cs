using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("check")]
public class CheckController : ControllerBase
{
    [HttpGet]
    public IActionResult answerCheck()
    {
        return Ok();
    }
}