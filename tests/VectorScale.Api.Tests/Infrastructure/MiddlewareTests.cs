using FluentAssertions;
using Microsoft.AspNetCore.Http;
using VectorScale.Api.Infrastructure.Middleware;

namespace VectorScale.Api.Tests.Infrastructure;

public class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-ID";

    [Fact]
    public async Task InvokeAsync_NoIncomingHeader_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContainsKey(HeaderName).Should().BeTrue();
        context.Response.Headers[HeaderName].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithIncomingHeader_EchoesSameId()
    {
        var existingId = "abc123def456";
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = existingId;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[HeaderName].ToString().Should().Be(existingId);
    }

    [Fact]
    public async Task InvokeAsync_SetsCorrelationIdInContextItems()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("CorrelationId").Should().BeTrue();
        context.Items["CorrelationId"].Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GeneratedId_Has16CharLength()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var id = context.Response.Headers[HeaderName].ToString();
        id.Should().HaveLength(16);
    }
}
