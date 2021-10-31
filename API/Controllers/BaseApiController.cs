using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [LogUserActivity]
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {

    }
}