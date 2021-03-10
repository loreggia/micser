using Microsoft.AspNetCore.Mvc;

namespace Micser.Common.Controllers
{
    /// <summary>
    /// Base class for API controllers.
    /// </summary>
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
    }
}