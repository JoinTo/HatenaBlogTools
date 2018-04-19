//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013-2018 smdn
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
using System.Net;
using System.Reflection;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  abstract class CliBase {
    protected bool ParseCommonCommandLineArgs(ref string[] args, out HatenaBlogAtomPubCredential credential)
    {
      credential = null;

      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      var unparsedArgs = new List<string>(args.Length);

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-id":
            hatenaId = args[++i];
            break;

          case "-blogid":
            blogId = args[++i];
            break;

          case "-apikey":
            apiKey = args[++i];
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default:
            unparsedArgs.Add(args[i]);
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId)) {
        Usage("hatena-idを指定してください");
        return false;
      }

      if (string.IsNullOrEmpty(blogId)) {
        Usage("blog-idを指定してください");
        return false;
      }

      if (string.IsNullOrEmpty(apiKey)) {
        Usage("api-keyを指定してください");
        return false;
      }

      credential = new HatenaBlogAtomPubCredential(hatenaId, blogId, apiKey);

      args = unparsedArgs.ToArray();

      return true;
    }

    protected HatenaBlogAtomPubClient CreateClient(HatenaBlogAtomPubCredential credential)
    {
      HatenaBlogAtomPubClient.InitializeHttpsServicePoint();

      var client = new HatenaBlogAtomPubClient(credential);

      client.UserAgent = $"{AssemblyInfo.Name}/{AssemblyInfo.Version} ({AssemblyInfo.TargetFramework}; {Environment.OSVersion.VersionString})";

      return client;
    }

    protected bool Login(HatenaBlogAtomPubCredential credential, out HatenaBlogAtomPubClient hatenaBlog)
    {
      hatenaBlog = CreateClient(credential);

      Console.Write("ログインしています ... ");

      var statusCode = hatenaBlog.Login(out _);

      if (statusCode == HttpStatusCode.OK) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("ログインに成功しました。");
        Console.ResetColor();

        return true;
      }
      else {
        hatenaBlog = null;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("ログインに失敗しました。　({0:D} {0})", statusCode);
        Console.ResetColor();

        return false;
      }
    }

    protected abstract string GetUsageExtraMandatoryOptions();

    protected abstract IEnumerable<string> GetUsageExtraOptionDescriptions();

    protected void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
        Console.ResetColor();

        Console.Error.WriteLine();
      }

      var assm = Assembly.GetEntryAssembly();
      var informationalVersion = (assm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0] as AssemblyInformationalVersionAttribute).InformationalVersion;

      var commandLine = $"dotnet {System.IO.Path.GetFileName(assm.Location)} --"; // TODO: on .NET Framework, etc.

      Console.Error.WriteLine($"{AssemblyInfo.Name} {AssemblyInfo.SubName} version {informationalVersion} (for {AssemblyInfo.TargetFramework})");
      Console.Error.WriteLine();
      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine($"  {commandLine} -id <hatena-id> -blogid <blog-id> -apikey <api-key> " + GetUsageExtraMandatoryOptions());
      Console.Error.WriteLine();
      Console.Error.WriteLine("  <hatena-id> : your Hatena id");
      Console.Error.WriteLine("  <blog-id>   : your blog domain name (xxx.hatenablog.jp, xxx.hateblo.jp, etc.)");
      Console.Error.WriteLine("  <api-key>   : AtomPub API key (see http://blog.hatena.ne.jp/my/config/detail)");
      Console.Error.WriteLine();

      Console.Error.WriteLine("options:");

      foreach (var extraOptionDescription in GetUsageExtraOptionDescriptions()) {
        Console.Error.WriteLine($"  {extraOptionDescription}");
      }

      Console.Error.WriteLine("  -h, --help, /help : show usage");

      Environment.Exit(-1);
    }
  }
}