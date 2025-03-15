// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using FileExplorerGitIntegration.Models;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class WslIntegratorUnitTests
{
    [TestMethod]
    [DataRow(@"\\wsl$\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"\\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"\\wsl$\Ubuntu\home\user\repo")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\user\repo")]
    [DataRow(@"\\wsl.localhost\Debian\home\user\repo")]
    [DataRow(@"\\wsl$\kali-linux\home\user\repo")]
    [DataRow(@"\\wsl$\Ubuntu-18.04\home\user\testRepo")]
    [DataRow(@"\\wsl.localhost\Ubuntu-18.04\home\user\testRepo")]
    [DataRow(@"\\WSL.LOCALHOST\Ubuntu-18.04\home\user\testRepo")]
    [DataRow(@"\\WSL$\Ubuntu-18.04\home\user\testRepo")]
    [DataRow(@"\\WsL.loCaLHoST\Ubuntu-18.04\home\user\testRepo")]
    [DataRow(@"\\WsL$\Ubuntu-18.04\home\user\testRepo")]
    public void IsWSLRepoPositiveTests(string repositoryPath)
    {
        Assert.IsTrue(WslIntegrator.IsWSLRepo(repositoryPath));
    }

    [TestMethod]
    [DataRow(@"//wsl$/kali-linux/home/user/repo")]
    [DataRow(@"C:\Users\foo\bar")]
    [DataRow(@"\\wsl$*\Ubuntu\home\user\repo")]
    [DataRow(@"D:\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"\\wsl.test\Ubuntu\home\user\repo")]
    [DataRow("")]
    [DataRow(@"\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"wsl$\Ubuntu-20.04\home\user\repo")]
    public void IsWslRepoNegativeTests(string repositoryPath)
    {
        Assert.IsFalse(WslIntegrator.IsWSLRepo(repositoryPath));
    }

    [TestMethod]
    [DataRow(@"\\wsl$\Ubuntu-20.04\home\user\repo", "Ubuntu-20.04")]
    [DataRow(@"\\wsl.localhost\Ubuntu-20.04\home\user\repo", "Ubuntu-20.04")]
    [DataRow(@"\\wsl$\Debian\home\user\repo", "Debian")]
    [DataRow(@"\\wsl.localhost\kali-linux\home\user\repo", "kali-linux")]
    [DataRow(@"\\wsl.localhost\UbuntuTest\home\user\testRepo", "UbuntuTest")]
    [DataRow(@"\\wsl$\CustomDistribution\home\user\testRepo", "CustomDistribution")]
    public void GetDistributionNamePositiveTest(string repositoryPath, string value)
    {
        var distributionName = WslIntegrator.GetWslDistributionName(repositoryPath);
        Assert.AreEqual(value, distributionName);
    }

    [TestMethod]
    [DataRow(@"C:\Distribution\home\user\testRepo")]
    [DataRow(@"\\Ubuntu-18.04\wsl$\home\user\testRepo")]
    [DataRow(@"wslg\Ubuntu-18.04\wsl.localhost\home\user\testRepo")]
    [DataRow("")]
    [DataRow(@"\\wsl$")]
    [DataRow(@"\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"wsl$\Ubuntu-20.04\home\user\repo")]
    public void GetDistributionNameNegativeTest(string repositoryPath)
    {
        Trace.Listeners.Clear();
        Assert.ThrowsException<ArgumentException>(() => WslIntegrator.GetWslDistributionName(repositoryPath));
    }

    [TestMethod]
    [DataRow(@"\\wsl$\Ubuntu-20.04\home\user\repo", @"\\wsl$\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"\\wsl.localhost\Ubuntu-20.04\home\user\repo", @"\\wsl$\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"\\wsl$\Debian\home\user\repo", @"\\wsl$\Debian\home\user\repo")]
    [DataRow(@"\\wsl.localhost\kali-linux\home\user\repo", @"\\wsl$\kali-linux\home\user\repo")]
    [DataRow(@"\\wsl.localhost\customDistribution\home\user\testRepo", @"\\wsl$\customDistribution\home\user\testRepo")]
    [DataRow(@"\\wsl$\Ubuntu-18.04\home\user\dir1\dir2\DIR3\testRepo", @"\\wsl$\Ubuntu-18.04\home\user\dir1\dir2\DIR3\testRepo")]
    public void GetWorkingDirectoryPositiveTest(string repositoryPath, string value)
    {
        var workingDirPath = WslIntegrator.GetWorkingDirectory(repositoryPath);
        Assert.AreEqual(value, workingDirPath);
    }

    [TestMethod]
    [DataRow(@"C:\Distribution\home\user\testRepo")]
    [DataRow(@"\\Ubuntu-18.04\wsl$\home\user\testRepo")]
    [DataRow(@"wslg\Ubuntu-18.04\wsl.localhost\home\user\testRepo")]
    [DataRow("")]
    [DataRow(@"\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"wsl$\Ubuntu-20.04\home\user\repo")]
    public void GetWorkingDirectoryNegativeTest(string repositoryPath)
    {
        Trace.Listeners.Clear();
        Assert.ThrowsException<ArgumentException>(() => WslIntegrator.GetWorkingDirectory(repositoryPath));
    }

    [TestMethod]
    [DataRow(@"\\wsl$\Ubuntu-20.04\home\user\repo", "-d Ubuntu-20.04 git ")]
    [DataRow(@"\\wsl.localhost\Ubuntu-20.04\home\user\repo", "-d Ubuntu-20.04 git ")]
    [DataRow(@"\\wsl$\Debian\home\user\repo", "-d Debian git ")]
    [DataRow(@"\\wsl.localhost\kali-linux\home\user\repo", "-d kali-linux git ")]
    [DataRow(@"\\wsl$\Ubuntu-18.04\home\user\testRepo", "-d Ubuntu-18.04 git ")]
    [DataRow(@"\\wsl.localhost\Ubuntu-18.04\home\user\testRepo", "-d Ubuntu-18.04 git ")]
    [DataRow(@"\\wsl.localhost\CustomDistribution\home\user\testRepo", "-d CustomDistribution git ")]
    public void GetArgumentPrefixForWslPositiveTest(string repositoryPath, string value)
    {
        var prefix = WslIntegrator.GetArgumentPrefixForWsl(repositoryPath);
        Assert.AreEqual(value, prefix);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(@"\\wsl.localhost")]
    [DataRow(@"C:\Users\foo\bar")]
    [DataRow(@"\wsl.localhost\Ubuntu-20.04\home\user\repo")]
    [DataRow(@"wsl$\Ubuntu-20.04\home\user\repo")]
    public void GetArgumentPrefixForWslNegativeTest(string repositoryPath)
    {
        Trace.Listeners.Clear();
        Assert.ThrowsException<ArgumentException>(() => WslIntegrator.GetArgumentPrefixForWsl(repositoryPath));
    }

    [TestMethod]
    [DataRow(@"\\wsl$\Ubuntu-20.04\home\user\repo", "/home/user/repo")]
    [DataRow(@"\\wsl.localhost\Ubuntu-20.04\home\user\repo", "/home/user/repo")]
    [DataRow(@"\\wsl$\Debian\home\user\repo", "/home/user/repo")]
    [DataRow(@"\\wsl.localhost\kali-linux\home\user\repo", "/home/user/repo")]
    [DataRow(@"\\WSL.LOCALHOST\UBUNTU-18.04\HOME\USER\TESTREPO", "/HOME/USER/TESTREPO")]
    [DataRow(@"\\WSL$\UBUNTU-18.04\HOME\USER\TESTREPO", "/HOME/USER/TESTREPO")]
    [DataRow(@"\\WSL.LOCALHOST\UBUNTU-18.04\HoME\USeR\TeSTREpO", "/HoME/USeR/TeSTREpO")]
    [DataRow(@"\\wsl.localhost\kali-linux\home\user\dir1\dir2\dir3\dir4\repo", "/home/user/dir1/dir2/dir3/dir4/repo")]
    [DataRow("", "")]
    public void GetNormalizedLinuxPathTest(string repositoryPath, string value)
    {
        var normalizedPath = WslIntegrator.GetNormalizedLinuxPath(repositoryPath);
        Assert.AreEqual(value, normalizedPath);
    }
}
