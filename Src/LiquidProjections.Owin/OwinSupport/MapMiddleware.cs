using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiquidProjections.Owin.Support
{
    internal class MapMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> next;
        private readonly MapOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Owin.Mapping.MapMiddleware" /> class
        /// </summary>
        /// <param name="next">The normal pipeline taken for a negative match</param>
        /// <param name="options"></param>
        public MapMiddleware(Func<IDictionary<string, object>, Task> next, MapOptions options)
        {
            this.next = next;
            this.options = options;
        }

        /// <summary>Process an individual request.</summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            PathString path = new PathString((string) environment["owin.RequestPath"]);
            PathString remainingPath;
            if (path.StartsWithSegments(options.PathMatch, out remainingPath))
            {
                string pathBase = (string) environment["owin.RequestPathBase"];
                environment["owin.RequestPathBase"] = pathBase + options.PathMatch.Value;
                environment["owin.RequestPath"] = remainingPath.Value;
                await options.Branch(environment);
                environment["owin.RequestPathBase"] = pathBase;
                environment["owin.RequestPath"] = path.Value;
            }
            else
            {
                await next(environment);
            }
        }
    }

    internal class MapOptions
    {
        /// <summary>The path to match</summary>
        public PathString PathMatch { get; set; }

        /// <summary>The branch taken for a positive match</summary>
        public Func<IDictionary<string, object>, Task> Branch { get; set; }
    }

    /// <summary>
    /// Provides correct escaping for Path and PathBase values when needed to reconstruct a request or redirect URI string
    /// </summary>
    internal struct PathString : IEquatable<PathString>
    {
        private static readonly Func<string, string> EscapeDataString = new Func<string, string>(Uri.EscapeDataString);

        /// <summary>Represents the empty path. This field is read-only.</summary>
        public static readonly PathString Empty = new PathString(string.Empty);

        /// <summary>The unescaped path value</summary>
        public string Value { get; }

        /// <summary>True if the path is not empty</summary>
        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(Value); }
        }

        /// <summary>
        /// Initialize the path string with a given value. This value must be in un-escaped format. Use
        /// PathString.FromUriComponent(value) if you have a path value which is in an escaped format.
        /// </summary>
        /// <param name="value">The unescaped path to be assigned to the Value property.</param>
        public PathString(string value)
        {
            if (!string.IsNullOrEmpty(value) && (int) value[0] != 47)
            {
                throw new ArgumentException("value");
            }
            Value = value;
        }

        /// <summary>Operator call through to Equals</summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are equal</returns>
        public static bool operator ==(PathString left, PathString right)
        {
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Operator call through to Equals</summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are not equal</returns>
        public static bool operator !=(PathString left, PathString right)
        {
            return !left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Operator call through to Add</summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static PathString operator +(PathString left, PathString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public string ToUriComponent()
        {
            if (!HasValue)
            {
                return string.Empty;
            }
            if (!RequiresEscaping(Value))
            {
                return Value;
            }
            return string.Join("/", Value.Split('/').Select(EscapeDataString));
        }

        private static bool RequiresEscaping(string value)
        {
            for (int index = 0; index < value.Length; ++index)
            {
                char ch = value[index];
                if ((97 > (int) ch || (int) ch > 122) && (65 > (int) ch || (int) ch > 90) &&
                    ((48 > (int) ch || (int) ch > 57) && ((int) ch != 47 && (int) ch != 45)) && (int) ch != 95)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns an PathString given the path as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a path.
        /// </summary>
        /// <param name="uriComponent">The escaped path as it appears in the URI format.</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(string uriComponent)
        {
            return new PathString(Uri.UnescapeDataString(uriComponent));
        }

        /// <summary>
        /// Returns an PathString given the path as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(Uri uri)
        {
            if (uri == (Uri) null)
            {
                throw new ArgumentNullException("uri");
            }
            return new PathString("/" + uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        }

        /// <summary>
        /// Checks if this instance starts with or exactly matches the other instance. Only full segments are matched.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool StartsWithSegments(PathString other)
        {
            string str1 = Value ?? string.Empty;
            string str2 = other.Value ?? string.Empty;
            if (!str1.StartsWith(str2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (str1.Length != str2.Length)
            {
                return (int) str1[str2.Length] == 47;
            }
            return true;
        }

        /// <summary>
        /// Checks if this instance starts with or exactly matches the other instance. Only full segments are matched.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="remaining">Any remaining segments from this instance not included in the other instance.</param>
        /// <returns></returns>
        public bool StartsWithSegments(PathString other, out PathString remaining)
        {
            string str1 = Value ?? string.Empty;
            string str2 = other.Value ?? string.Empty;
            if (str1.StartsWith(str2, StringComparison.OrdinalIgnoreCase) &&
                (str1.Length == str2.Length || (int) str1[str2.Length] == 47))
            {
                remaining = new PathString(str1.Substring(str2.Length));
                return true;
            }
            remaining = Empty;
            return false;
        }

        /// <summary>
        /// Adds two PathString instances into a combined PathString value.
        /// </summary>
        /// <returns>The combined PathString value</returns>
        public PathString Add(PathString other)
        {
            return new PathString(Value + other.Value);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="other">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares this PathString value to another value using a specific StringComparison type
        /// </summary>
        /// <param name="other">The second PathString for comparison</param>
        /// <param name="comparisonType">The StringComparison type to use</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other, StringComparison comparisonType)
        {
            return string.Equals(Value, other.Value, comparisonType);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="obj">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals((object) null, obj) || !(obj is PathString))
            {
                return false;
            }
            return Equals((PathString) obj, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the hash code for the PathString value. The hash code is provided by the OrdinalIgnoreCase implementation.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }
    }
}