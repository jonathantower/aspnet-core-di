using System.Threading.Tasks;
using code.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace code.Middleware
{
	public class RequestIdMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IRequestId _requestId;
		private readonly ILogger<RequestIdMiddleware> _logger;

		public RequestIdMiddleware(RequestDelegate next, IRequestId requestId, ILogger<RequestIdMiddleware> logger)
		{
			_next = next;
			_requestId = requestId;
			_logger = logger;
		}

		public Task Invoke(HttpContext context)
		{
			_logger.LogInformation($"Request {_requestId.Id} executing.");

			return _next(context);
		}
	}
}
