using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Domain.Tests;

public class WorkspaceFileStorageModelTests
{
    [Fact]
    public void AssetKind_RepresentsCommonCreativeAssetCategories()
    {
        var supportedKinds = Enum.GetValues<AssetKind>();

        Assert.Contains(AssetKind.SourceDesign, supportedKinds);
        Assert.Contains(AssetKind.ExportedImage, supportedKinds);
        Assert.Contains(AssetKind.Svg, supportedKinds);
        Assert.Contains(AssetKind.MockupImage, supportedKinds);
        Assert.Contains(AssetKind.ReferenceImage, supportedKinds);
        Assert.Contains(AssetKind.Texture, supportedKinds);
        Assert.Contains(AssetKind.Brush, supportedKinds);
        Assert.Contains(AssetKind.Font, supportedKinds);
        Assert.Contains(AssetKind.PromptOutput, supportedKinds);
        Assert.Contains(AssetKind.ExternalLink, supportedKinds);
        Assert.Contains(AssetKind.Unknown, supportedKinds);
        Assert.Contains(AssetKind.Other, supportedKinds);
    }

    [Fact]
    public void WorkspaceFileReference_NormalizesSeparatorsAndRejectsEscapingPaths()
    {
        var reference = new WorkspaceFileReference(@"assets\2026\06\design.png");

        Assert.Equal("assets/2026/06/design.png", reference.WorkspaceRelativePath);
        Assert.Equal("assets/2026/06/design.png", reference.ToString());
        Assert.Throws<ArgumentException>(() => new WorkspaceFileReference(""));
        Assert.Throws<ArgumentException>(() => new WorkspaceFileReference("   "));
        Assert.Throws<ArgumentException>(() => new WorkspaceFileReference("../design.png"));
        Assert.Throws<ArgumentException>(() => new WorkspaceFileReference("assets/../design.png"));
        Assert.Throws<ArgumentException>(() => new WorkspaceFileReference(Path.GetFullPath("design.png")));
    }

    [Fact]
    public void Asset_StoresWorkspaceReferenceMetadataWithoutFileBytes()
    {
        var asset = new Asset(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Design export",
            "Primary PNG export",
            AssetKind.ExportedImage,
            @"assets\2026\06\design.png",
            @"C:\imports\design.png",
            isMissing: false,
            isArchived: false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            """{"itemId":"draft"}""");

        Assert.Equal("assets/2026/06/design.png", asset.WorkspaceRelativePath);
        Assert.Equal(@"C:\imports\design.png", asset.OriginalSourcePath);
        Assert.Equal("""{"itemId":"draft"}""", asset.MetadataJson);
        Assert.DoesNotContain(asset.GetType().GetProperties(), property => property.Name.Contains("Bytes", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(asset.GetType().GetProperties(), property => property.Name.Contains("Content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssetLink_CanConnectManagedFilesToCreativeContextWithoutAdvancedEntities()
    {
        var assetId = Guid.NewGuid();
        var supportedTargets = new[]
        {
            WorkspaceEntityKind.Store,
            WorkspaceEntityKind.Niche,
            WorkspaceEntityKind.Group,
            WorkspaceEntityKind.Item,
            WorkspaceEntityKind.Asset,
            WorkspaceEntityKind.Prompt,
            WorkspaceEntityKind.Design,
            WorkspaceEntityKind.FutureRelatedRecord
        };

        var links = supportedTargets
            .Select(target => new AssetLink(assetId, target, Guid.NewGuid()))
            .ToArray();

        Assert.All(links, link => Assert.Equal(assetId, link.AssetId));
        Assert.Throws<ArgumentException>(() => new AssetLink(Guid.Empty, WorkspaceEntityKind.Item, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new AssetLink(assetId, WorkspaceEntityKind.Item, Guid.Empty));
    }

    [Fact]
    public void WorkspaceFileStorageModel_DoesNotIntroduceAdvancedWorkflowTypes()
    {
        var workspaceTypeNames = typeof(Asset).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "FusionCanvas.Domain.Workspace")
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("BatchAssetImport", workspaceTypeNames);
        Assert.DoesNotContain("FileDeduplication", workspaceTypeNames);
        Assert.DoesNotContain("CloudSync", workspaceTypeNames);
        Assert.DoesNotContain("ImageProcessor", workspaceTypeNames);
        Assert.DoesNotContain("MockupGenerator", workspaceTypeNames);
        Assert.DoesNotContain("FileRepair", workspaceTypeNames);
        Assert.DoesNotContain("FileVersionHistory", workspaceTypeNames);
        Assert.DoesNotContain("MarketplaceExportPackage", workspaceTypeNames);
    }
}
