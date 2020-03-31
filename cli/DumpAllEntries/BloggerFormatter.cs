//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2020 smdn
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
using System.Linq;
using System.IO;
using System.Xml.Linq;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;
using Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol;

namespace Smdn.Applications.HatenaBlogTools {
  class BloggerFormatter : FormatterBase {
    private readonly string blogTitle;

    public BloggerFormatter(string blogTitle = null)
    {
      this.blogTitle = blogTitle;
    }

    public override void Format(IEnumerable<PostedEntry> entries, Stream outputStream)
    {
      var elementListEntry = entries.Select(entry =>
        // ./entry
        new XElement(
          AtomPub.Namespaces.Atom + "entry",
          // ./entry/id
          entry.Id == null
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "id",
                entry.Id
              ),
          // ./entry/author
          string.IsNullOrEmpty(entry.Author)
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "author",
                new XElement(
                  AtomPub.Namespaces.Atom + "name",
                  entry.Author
                )
              ),
          // ./entry/published
          entry.DatePublished == null
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "published",
                entry.DatePublished
              ),
          // ./entry/updated
          entry.DateUpdated == null
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "updated",
                entry.DateUpdated
              ),
          // ./entry/title
          new XElement(
            AtomPub.Namespaces.Atom + "title",
            new XAttribute("type", "text"),
            entry.Title
          ),
          // ./entry/control
          entry.IsDraft
            ? new XElement(
                AtomPub.Namespaces.App + "control",
                // ./entry/control/draft
                new XElement(
                  AtomPub.Namespaces.App + "draft",
                  "yes"
                )
              )
            : null,
          // ./entry/category
          new XElement(
            AtomPub.Namespaces.Atom + "category",
            new XAttribute("scheme", "http://schemas.google.com/g/2005#kind"),
            new XAttribute("term", "http://schemas.google.com/blogger/2008/kind#post")
          ),
          entry.Categories.Select(category =>
            new XElement(
              AtomPub.Namespaces.Atom + "category",
              new XAttribute("scheme", "http://www.blogger.com/atom/ns#"),
              new XAttribute("term", category)
            )
          ),
          // ./entry/content
          new XElement(
            AtomPub.Namespaces.Atom + "content",
            new XAttribute("type", "html"),
            entry.FormattedContent
          )
        )
      );

      var document = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        // /feed
        new XElement(
          AtomPub.Namespaces.Atom + "feed",
          //new XAttribute(XNamespace.Xmlns + string.Empty, AtomPub.Namespaces.Atom),
          new XAttribute(XNamespace.Xmlns + "app", AtomPub.Namespaces.App),
          // /feed/title
          string.IsNullOrEmpty(blogTitle)
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "title",
                blogTitle
              ),
          // /feed/generator
          new XElement(
            AtomPub.Namespaces.Atom + "generator",
            "Blogger"
          ),
          // /feed/entry
          elementListEntry
        )
      );

      document.Save(outputStream);
    }
  }
}
