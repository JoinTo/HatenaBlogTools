using System;
using System.Collections.Generic;

namespace Smdn.Applications.HatenaBlogTools.HatenaBlog {
  class PseudoPostedEntry : PostedEntry {
    public PseudoPostedEntry(
      Uri id = null,
      Uri memberUri = null,
      Uri entryUri = null,
      DateTimeOffset datePublished = default,
      IEnumerable<string> categories = null,
      string formattedContent = null
    )
      : base(
        id: id,
        memberUri: memberUri,
        entryUri: entryUri,
        published: datePublished,
        categories: categories,
        formattedContent: formattedContent
      )
    {
    }
  }
}
