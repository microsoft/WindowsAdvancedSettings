// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerSourceControlIntegration.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.FileExplorerSourceControlIntegrationUnitTest;

[TestClass]
public class RepositoryTrackingServiceUnitTest
{
    private RepositoryTracking RepoTracker { get; set; } = new(Path.Combine(Path.GetTempPath()));

    private readonly string _extension = "testExtension";

    private readonly string _rootPath = "c:\\test\\rootPath";

    private readonly string _caseAlteredRootPath = "C:\\TEST\\ROOTPATH";

    [TestMethod]
    public void AddRepository()
    {
        RepoTracker.AddRepositoryPath(_extension, _rootPath);
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey(_rootPath));
        Assert.IsTrue(result.ContainsValue(_extension));
        RepoTracker.RemoveRepositoryPath(_rootPath);
    }

    [TestMethod]
    public void RemoveRepository()
    {
        RepoTracker.AddRepositoryPath(_extension, _rootPath);
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.AreEqual(1, result.Count);
        RepoTracker.RemoveRepositoryPath(_rootPath);
        result = RepoTracker.GetAllTrackedRepositories();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetAllRepositories()
    {
        for (var i = 0; i < 5; i++)
        {
            RepoTracker.AddRepositoryPath(_extension, string.Concat(_rootPath, i));
        }

        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count);
        Assert.IsTrue(result.ContainsKey(string.Concat(_rootPath, 0)));
        Assert.IsTrue(result.ContainsValue(_extension));

        for (var i = 0; i < 5; i++)
        {
            RepoTracker.RemoveRepositoryPath(string.Concat(_rootPath, i));
        }
    }

    [TestMethod]
    public void GetAllRepositories_Empty()
    {
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetSourceControlProviderFromRepositoryPath()
    {
        RepoTracker.AddRepositoryPath(_extension, _rootPath);
        var result = RepoTracker.GetSourceControlProviderForRootPath(_rootPath);
        Assert.IsNotNull(result);
        Assert.AreEqual(_extension, result);
        RepoTracker.RemoveRepositoryPath(_rootPath);
    }

    [TestMethod]
    public void AddRepository_DoesNotAddDuplicateValues()
    {
        RepoTracker.AddRepositoryPath(_extension, _rootPath);

        // Atempt to add duplicate entry that is altered in case
        RepoTracker.AddRepositoryPath(_extension, _caseAlteredRootPath);

        // Ensure duplicate is not added and count is 1
        var result = RepoTracker.GetAllTrackedRepositories();
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey(_rootPath));
        Assert.IsTrue(result.ContainsValue(_extension));

        RepoTracker.RemoveRepositoryPath(_rootPath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(Path.Combine(Path.GetTempPath(), "TrackedRepositoryStore.json")))
        {
            File.Delete(Path.Combine(Path.GetTempPath(), "TrackedRepositoryStore.json"));
        }
    }
}
