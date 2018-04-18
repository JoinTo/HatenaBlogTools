//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2014 smdn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  partial class MainClass {
    private static string GetUsageExtraMandatoryOptions() => "-from 'oldtext' [-to 'newtext']";

    private static IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "-from <oldtext>       : text to be replaced";
      yield return "-to <newtext>         : text to replace <oldtext>";
      yield return "-regex                : treat <oldtext> and <newtext> as regular expressions";
      yield return "-diff-cmd <command>   : use <command> as diff command";
      yield return "-diff-cmd-args <args> : specify arguments for diff command";
      yield return "-v                    : display replacement result";
      yield return "-n                    : dry run";
    }

    public static void Main(string[] args)
    {
      HatenaBlogAtomPubClient.InitializeHttpsServicePoint();

      if (!ParseCommonCommandLineArgs(ref args, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      string replaceFromText = null;
      string replaceToText = null;
      bool replaceAsRegex = false;
      string diffCommand = null;
      string diffCommandArgs = null;
      bool verbose = false;
      bool dryrun = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-from":
            replaceFromText = args[++i];
            break;

          case "-to":
            replaceToText = args[++i];
            break;

          case "-regex":
            replaceAsRegex = true;
            break;

          case "-diff-cmd":
            diffCommand = args[++i];
            break;

          case "-diff-cmd-args":
            diffCommandArgs = args[++i];
            break;

          case "-v":
            verbose = true;
            break;

          case "-n":
            dryrun = true;
            break;
        }
      }

      if (string.IsNullOrEmpty(replaceFromText)) {
        Usage("置換する文字列を指定してください");
        return;
      }

      if (replaceToText == null)
        replaceToText = string.Empty; // delete

      var editor = replaceAsRegex
        ? (IHatenaBlogEntryEditor)new RegexEntryEditor(replaceFromText, replaceToText)
        : (IHatenaBlogEntryEditor)new EntryEditor(replaceFromText, replaceToText);

      var diffGenerator = DiffGenerator.Create(!verbose,
                                               diffCommand,
                                               diffCommandArgs,
                                               "置換前の本文",
                                               "置換後の本文");

      if (!diffGenerator.IsAvailable()) {
        Usage("指定されたdiffコマンドは利用できません。　コマンドのパスと、カレントディレクトリに書き込みができることを確認してください。");
        return;
      }

      var postMode = dryrun
        ? HatenaBlogFunctions.PostMode.PostNever
        : HatenaBlogFunctions.PostMode.PostIfModified;

      if (!Login(hatenaBlog))
        return;

      HatenaBlogFunctions.EditAllEntryContent(hatenaBlog,
                                              postMode,
                                              editor,
                                              diffGenerator);
    }

    private class EntryEditor : IHatenaBlogEntryEditor {
      private readonly string replaceFrom;
      private readonly string replaceTo;

      public EntryEditor(string replaceFrom, string replaceTo)
      {
        this.replaceFrom = replaceFrom;
        this.replaceTo = replaceTo;
      }

      public void Edit(PostedEntry entry, Action<string, string> actionIfModified)
      {
        var originalContent = entry.Content;

        entry.Content = originalContent.Replace(replaceFrom, replaceTo);

        var modified =
          (originalContent.Length != entry.Content.Length) ||
          !string.Equals(originalContent, entry.Content, StringComparison.Ordinal);

        if (modified)
          actionIfModified?.Invoke(originalContent, entry.Content);
      }
    }

    private class RegexEntryEditor : IHatenaBlogEntryEditor {
      private readonly Regex regexToReplace;
      private readonly string replacement;

      public RegexEntryEditor(string regexToReplace, string replacement)
      {
        this.regexToReplace = new Regex(regexToReplace, RegexOptions.Multiline);
        this.replacement = replacement;
      }

      public void Edit(PostedEntry entry, Action<string, string> actionIfModified)
      {
        var modified = false;
        var originalContent = entry.Content;

        entry.Content = regexToReplace.Replace(originalContent, (match) => {
          modified |= true;

          return match.Result(replacement);
        });

        if (modified)
          actionIfModified?.Invoke(originalContent, entry.Content);
      }
    }
  }
}
