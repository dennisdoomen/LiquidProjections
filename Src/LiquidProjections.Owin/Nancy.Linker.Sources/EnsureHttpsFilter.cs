using System;
using Nancy;

namespace LiquidProjections.Owin.Nancy.Linker.Sources
{
    public class EnsureHttpsFilter : UriFilter
  {
    public EnsureHttpsFilter(IUriFilter nextFilter = null) : 
      base(nextFilter)
    {
    }

    protected override Uri OnApply(Uri uri, NancyContext context)
    {
      if (string.Compare(uri.Scheme, "https", true) == 0) return uri;

      UriBuilder builder = new UriBuilder(uri);
      builder.Scheme = "https";
      builder.Port = 443;
      return builder.Uri;
    }
  }
}
