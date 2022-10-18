using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

using Smdn.IO;
using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools {
  [TestFixture]
  public class MovableTypeFormatterTests {
    private static System.Collections.IEnumerable YieldTestCases_TestToDateString_DateTime()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      yield return new object[] { new DateTime(2020, 3, 31, 0, 1, 2), "03/31/2020 12:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 1, 1, 2), "03/31/2020 01:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 11, 1, 2), "03/31/2020 11:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 12, 1, 2), "03/31/2020 12:01:02 PM" };
      yield return new object[] { new DateTime(2020, 3, 31, 13, 1, 2), "03/31/2020 01:01:02 PM" };
      yield return new object[] { new DateTime(2020, 3, 31, 23, 1, 2), "03/31/2020 11:01:02 PM" };
    }

    [TestCaseSource(nameof(YieldTestCases_TestToDateString_DateTime))]
    public void TestToDateString_DateTime(DateTime dateTime, string expected)
      => Assert.AreEqual(expected, MovableTypeFormatter.ToDateString(dateTime));

    private static System.Collections.IEnumerable YieldTestCases_TestToDateString_DateTimeOffset()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(-5)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 1, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 01:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 11, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 11:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(+9)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(-5)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 13, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 01:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 23, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 11:00:00 PM" };
    }

    [TestCaseSource(nameof(YieldTestCases_TestToDateString_DateTimeOffset))]
    public void TestToDateString_DateTimeOffset(DateTimeOffset dateTimeOffset, string expected)
      => Assert.AreEqual(expected, MovableTypeFormatter.ToDateString(dateTimeOffset));

    private static void Format(
      IEnumerable<PostedEntry> entries,
      out string formattedText,
      out IReadOnlyList<string> formattedLines
    )
    {
      using (var stream = new MovableTypeFormatter().ToStream(entries)) {
        formattedLines = new StreamReader(stream, Encoding.UTF8).ReadAllLines();
        formattedText = string.Join("\n", formattedLines);
      }
    }

    [Test]
    public void TestFormat_SingleEntry()
    {
      var entry = new PseudoPostedEntry(
        id: new Uri("tag:blog.example.com,2020:entry0"),
        memberUri: new Uri("https://blog.example.com/atom/entry/0/"),
        entryUri: new Uri("https://example.com/entry/0/"),
        datePublished: new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(0)),
        authors: new[] {"entry0-author0", "entry0-author1"},
        categories: new[] {"entry0-category0", "entry0-category1", "entry0-category2"},
        formattedContent: "entry0-formatted-content"
      ) {
        Title = "entry0",
        DateUpdated = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)),
        IsDraft = false,
        Summary = "entry0-summary",
        Content = "entry0-content",
        ContentType = "text/x-hatena-syntax",
      };

      const string expectedResult = @"AUTHOR: entry0-author0 entry0-author1
TITLE: entry0
BASENAME: 0/
STATUS: Publish
CONVERT BREAKS: 0
DATE: 03/31/2020 12:00:00 AM
TAGS: entry0-category0,entry0-category1,entry0-category2
-----
BODY:
entry0-formatted-content
-----
--------";

      Format(new[] { entry }, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText
      );
    }

    [Test]
    public void TestFormat_MultipleEntries()
    {
      var entries = new[] {
        new PseudoPostedEntry(
          formattedContent: "entry0-formatted-content"
        ) {
          Title = "entry0",
          Content = "entry0-content",
          IsDraft = false,
        },
        new PseudoPostedEntry(
          formattedContent: "entry1-formatted-content"
        ) {
          Title = "entry1",
          Content = "entry1-content",
          IsDraft = true,
        },
      };

      const string expectedResult = @"AUTHOR:
TITLE: entry0
STATUS: Publish
CONVERT BREAKS: 0
TAGS:
-----
BODY:
entry0-formatted-content
-----
--------
AUTHOR:
TITLE: entry1
STATUS: Draft
CONVERT BREAKS: 0
TAGS:
-----
BODY:
entry1-formatted-content
-----
--------";

      Format(entries, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText.Replace(" \n", "\n") // trim line endings
      );
    }
  }
}
