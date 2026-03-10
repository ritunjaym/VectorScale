using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using VectorScale.Api.Models;

namespace VectorScale.Api.Tests.Models;

public class SearchModelsTests
{
    private static bool TryValidate(object model, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, true);
    }

    [Fact]
    public void SearchRequest_EmptyQuery_FailsValidation()
    {
        var request = new SearchRequest { Query = "", TopK = 5 };

        var isValid = TryValidate(request, out var results);

        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(SearchRequest.Query)));
    }

    [Fact]
    public void SearchRequest_ValidQuery_PassesValidation()
    {
        var request = new SearchRequest { Query = "test query", TopK = 10 };

        var isValid = TryValidate(request, out var results);

        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchRequest_TopKOutOfRange_FailsValidation()
    {
        var request = new SearchRequest { Query = "test", TopK = 0 };

        var isValid = TryValidate(request, out var results);

        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(SearchRequest.TopK)));
    }

    [Fact]
    public void SearchRequest_TopKAtMaxBoundary_PassesValidation()
    {
        var request = new SearchRequest { Query = "test", TopK = 100 };

        var isValid = TryValidate(request, out var results);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void SearchRequest_QueryExceedsMaxLength_FailsValidation()
    {
        var request = new SearchRequest { Query = new string('x', 2001), TopK = 5 };

        var isValid = TryValidate(request, out var results);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchResponse_DefaultValues_AreCorrect()
    {
        var response = new SearchResponse();

        response.Results.Should().BeEmpty();
        response.CacheHit.Should().BeFalse();
        response.Page.Should().Be(0);
    }

    [Fact]
    public void SearchResultItem_DefaultMetadata_IsEmpty()
    {
        var item = new SearchResultItem();

        item.Metadata.Should().NotBeNull();
        item.Metadata.Should().BeEmpty();
    }
}
