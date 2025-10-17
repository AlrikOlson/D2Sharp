using System.Diagnostics;
using Xunit;

namespace D2Sharp.Tests.Telemetry;

/// <summary>
/// Tests for D2SharpActivitySource distributed tracing component.
/// </summary>
public class D2SharpActivitySourceTests
{
    [Fact]
    public void SourceName_Constant_IsD2Sharp()
    {
        // Arrange & Act
        var sourceName = D2Sharp.Telemetry.D2SharpActivitySource.SourceName;

        // Assert
        Assert.Equal("D2Sharp", sourceName);
    }

    [Fact]
    public void Version_Constant_HasValue()
    {
        // Arrange & Act
        var version = D2Sharp.Telemetry.D2SharpActivitySource.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.Matches(@"^\d+\.\d+\.\d+$", version); // Semantic version format
    }

    [Fact]
    public void Source_Instance_IsNotNull()
    {
        // Arrange & Act
        var source = D2Sharp.Telemetry.D2SharpActivitySource.Source;

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void Source_Name_MatchesConstant()
    {
        // Arrange & Act
        var source = D2Sharp.Telemetry.D2SharpActivitySource.Source;

        // Assert
        Assert.Equal(D2Sharp.Telemetry.D2SharpActivitySource.SourceName, source.Name);
    }

    [Fact]
    public void Source_Version_MatchesConstant()
    {
        // Arrange & Act
        var source = D2Sharp.Telemetry.D2SharpActivitySource.Source;

        // Assert
        Assert.Equal(D2Sharp.Telemetry.D2SharpActivitySource.Version, source.Version);
    }

    [Fact]
    public void Tags_ScriptLength_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.ScriptLength;

        // Assert
        Assert.Equal("d2sharp.script.length", tag);
    }

    [Fact]
    public void Tags_LayoutEngine_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.LayoutEngine;

        // Assert
        Assert.Equal("d2sharp.layout.engine", tag);
    }

    [Fact]
    public void Tags_ThemeId_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.ThemeId;

        // Assert
        Assert.Equal("d2sharp.theme.id", tag);
    }

    [Fact]
    public void Tags_SketchMode_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.SketchMode;

        // Assert
        Assert.Equal("d2sharp.sketch.enabled", tag);
    }

    [Fact]
    public void Tags_DiagnosticId_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.DiagnosticId;

        // Assert
        Assert.Equal("d2sharp.diagnostic.id", tag);
    }

    [Fact]
    public void Tags_CacheHit_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.CacheHit;

        // Assert
        Assert.Equal("d2sharp.cache.hit", tag);
    }

    [Fact]
    public void Tags_ResultStatus_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.ResultStatus;

        // Assert
        Assert.Equal("d2sharp.result.status", tag);
    }

    [Fact]
    public void Tags_ErrorType_HasValue()
    {
        // Arrange & Act
        var tag = D2Sharp.Telemetry.D2SharpActivitySource.Tags.ErrorType;

        // Assert
        Assert.Equal("d2sharp.error.type", tag);
    }

    [Fact]
    public void Source_CanCreateActivity()
    {
        // Arrange
        var source = D2Sharp.Telemetry.D2SharpActivitySource.Source;

        // Act - Create activity (may return null if no listener)
        using var activity = source.StartActivity("test-activity");

        // Assert - Either activity is created or null (both are valid)
        // This tests that the ActivitySource is properly initialized
        Assert.True(true);
    }

    [Fact]
    public void Source_CanCreateActivityWithTags()
    {
        // Arrange
        var source = D2Sharp.Telemetry.D2SharpActivitySource.Source;
        var tags = new ActivityTagsCollection
        {
            { D2Sharp.Telemetry.D2SharpActivitySource.Tags.ScriptLength, 100 },
            { D2Sharp.Telemetry.D2SharpActivitySource.Tags.LayoutEngine, "dagre" },
            { D2Sharp.Telemetry.D2SharpActivitySource.Tags.ThemeId, 1 }
        };

        // Act
        using var activity = source.StartActivity("test-render", ActivityKind.Internal, default(ActivityContext), tags);

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void AllTagConstants_AreUnique()
    {
        // Arrange
        var tags = new[]
        {
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ScriptLength,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.LayoutEngine,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ThemeId,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.SketchMode,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.DiagnosticId,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.CacheHit,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ResultStatus,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ErrorType
        };

        // Act
        var uniqueTags = tags.Distinct().ToList();

        // Assert - All tags should be unique
        Assert.Equal(tags.Length, uniqueTags.Count);
    }

    [Fact]
    public void AllTagConstants_FollowNamingConvention()
    {
        // Arrange
        var tags = new[]
        {
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ScriptLength,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.LayoutEngine,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ThemeId,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.SketchMode,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.DiagnosticId,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.CacheHit,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ResultStatus,
            D2Sharp.Telemetry.D2SharpActivitySource.Tags.ErrorType
        };

        // Act & Assert - All tags should start with "d2sharp."
        foreach (var tag in tags)
        {
            Assert.StartsWith("d2sharp.", tag);
        }
    }
}
