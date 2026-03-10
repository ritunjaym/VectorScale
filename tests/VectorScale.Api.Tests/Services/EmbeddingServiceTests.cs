using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VectorScale.Api.Protos;
using ApiServices = VectorScale.Api.Services;
using ProtoServices = VectorScale.Api.Protos;

namespace VectorScale.Api.Tests.Services;

public class EmbeddingServiceTests
{
    private readonly Mock<ProtoServices.EmbeddingService.EmbeddingServiceClient> _grpcClientMock = new();
    private readonly ApiServices.EmbeddingService _sut;

    public EmbeddingServiceTests()
    {
        _sut = new ApiServices.EmbeddingService(_grpcClientMock.Object, NullLogger<ApiServices.EmbeddingService>.Instance);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ReturnsVector()
    {
        var expectedVector = Enumerable.Range(0, 384).Select(i => (float)i / 384).ToArray();
        var response = new EmbeddingResponse { Dimension = 384, LatencyMs = 10 };
        response.Vector.AddRange(expectedVector);

        var asyncUnaryCall = new AsyncUnaryCall<EmbeddingResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcClientMock
            .Setup(c => c.GenerateEmbeddingAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var result = await _sut.GenerateEmbeddingAsync("test query");

        result.Should().NotBeNull();
        result.Should().HaveCount(384);
        result.Should().BeEquivalentTo(expectedVector);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_GrpcThrows_PropagatesException()
    {
        var asyncUnaryCall = new AsyncUnaryCall<EmbeddingResponse>(
            Task.FromException<EmbeddingResponse>(new RpcException(new Status(StatusCode.Unavailable, "gRPC unavailable"))),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcClientMock
            .Setup(c => c.GenerateEmbeddingAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var act = async () => await _sut.GenerateEmbeddingAsync("test query");

        await act.Should().ThrowAsync<RpcException>();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SendsCorrectModelName()
    {
        EmbeddingRequest? capturedRequest = null;
        var response = new EmbeddingResponse { Dimension = 384, LatencyMs = 5 };
        response.Vector.AddRange(new float[384]);

        var asyncUnaryCall = new AsyncUnaryCall<EmbeddingResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcClientMock
            .Setup(c => c.GenerateEmbeddingAsync(
                It.IsAny<EmbeddingRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmbeddingRequest, Metadata, DateTime?, CancellationToken>((req, _, _, _) => capturedRequest = req)
            .Returns(asyncUnaryCall);

        await _sut.GenerateEmbeddingAsync("hello world");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Text.Should().Be("hello world");
        capturedRequest.ModelName.Should().Be("all-MiniLM-L6-v2");
    }
}
