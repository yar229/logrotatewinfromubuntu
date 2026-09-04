using System;
using System.Collections.Generic;
using System.IO;
//using System.Runtime.Remoting.Contexts;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal abstract class XBaseFile
{
    public XBaseFile(string testDir)
    {
        _testDir = testDir;
    }

    protected readonly string _testDir;
    protected string _content = string.Empty;

    public abstract string Type { get; }

    public string Filename { get; protected set; }

    public string Filepath
        => Path.Combine(_testDir, Filename);


    public bool Exists()
        => File.Exists(Filepath);

    public bool Exists(string append)
        => File.Exists($"{Filepath}{append}");


    public XBaseFile WithContent(string content)
    {
        _content = content;
        return this;
    }

    #region ShouldBe ========================================================================
    public readonly List<string> ShouldBeList = new();
 

    public XBaseFile ShouldBe(params string[] postfixes)
    {
        if (null == postfixes || postfixes.Length == 0)
        {
            ShouldBeList.Add(string.Empty);
            return this;
        }

        ShouldBeList.AddRange(postfixes);
        return this;
    }

    public List<string> ShouldNotBeList = new();
    public XBaseFile ShouldNotBe(params string[] postfixes)
    {
        if (null == postfixes || postfixes.Length == 0)
        {    
            ShouldNotBeList.Add(string.Empty);
            return this;
        }
        ShouldNotBeList.AddRange(postfixes);
        return this;
    }

    public List<string> ShouldNotContainList = new();
    public XBaseFile ShouldNotContain(params string[] values)
    {
        ShouldNotContainList.AddRange(values);
        return this;
    }

    public List<string> ShouldContainList = new();
    public XBaseFile ShouldContain(params string[] values)
    {
        ShouldContainList.AddRange(values);
        return this;
    }
    #endregion ShouldBe =====================================================================





    public static implicit operator string(XBaseFile stateFile)
        => stateFile.ToString();

    public override string ToString()
        => Filepath;
}
