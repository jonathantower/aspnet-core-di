using code.Services;
using Microsoft.AspNetCore.Mvc;

namespace code.Controllers
{
    public class HomeController : Controller
    {
	    private readonly IRequestId _requestId;

	    public HomeController(IRequestId requestId)
	    {
		    _requestId = requestId;
	    }

        public IActionResult Index()
        {
	        ViewData["RequestId"] = _requestId.Id;
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
