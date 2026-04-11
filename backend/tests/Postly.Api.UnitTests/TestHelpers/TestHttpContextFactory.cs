using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Postly.Api.UnitTests.TestHelpers;

public static class TestHttpContextFactory
{
    public static HttpContext CreateMockHttpContext(string traceIdentifier = "test-trace-id")
    {
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.TraceIdentifier).Returns(traceIdentifier);

        var mockResponse = new Mock<HttpResponse>();
        var mockResponseCookies = new Mock<IResponseCookies>();
        mockResponse.Setup(x => x.Cookies).Returns(mockResponseCookies.Object);
        mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);

        // Mock RequestServices for authentication
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockAuthenticationService = new Mock<IAuthenticationService>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(mockAuthenticationService.Object);
        mockHttpContext.Setup(x => x.RequestServices).Returns(mockServiceProvider.Object);

        return mockHttpContext.Object;
    }
}
