using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public static class XssFilter
{
    static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "a","b","strong","i","em","u","p","br","ul","ol","li","blockquote","pre","code",
        "span","div","img","h1","h2","h3","h4","h5","h6","table","thead","tbody","tr","th","td","hr"
    };

    static readonly Dictionary<string, HashSet<string>> AllowedAttrs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["*"] = new HashSet<string>(new[] { "class","id","title","aria-label","role","data-*"}, StringComparer.OrdinalIgnoreCase),
        ["a"] = new HashSet<string>(new[] { "href","target","rel" }, StringComparer.OrdinalIgnoreCase),
        ["img"] = new HashSet<string>(new[] { "src","alt","width","height" }, StringComparer.OrdinalIgnoreCase),
        ["table"] = new HashSet<string>(new[] { "border","cellpadding","cellspacing" }, StringComparer.OrdinalIgnoreCase),
        ["th"] = new HashSet<string>(new[] { "colspan","rowspan","scope" }, StringComparer.OrdinalIgnoreCase),
        ["td"] = new HashSet<string>(new[] { "colspan","rowspan" }, StringComparer.OrdinalIgnoreCase)
    };

    static readonly HashSet<string> DangerousTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script","noscript","iframe","object","embed","link","meta","base","form","input","button","select","textarea","style","svg","math"
    };

    static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http","https","mailto","tel"
    };

    static readonly Regex DataImageRegex = new(@"^data:image/(png|jpe?g|gif|webp);base64,[a-z0-9+/=\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex OnAttrRegex = new(@"^on[a-z0-9_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex DataAttrWildcard = new(@"^data\-[\w\-:.]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex CssExpressionRegex = new(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex UrlJsRegex = new(@"url\s*\(\s*javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        var doc = new HtmlDocument();
        doc.OptionFixNestedTags = true;
        doc.LoadHtml(html);

        RemoveCommentsAndDocType(doc);
        RemoveDangerousNodes(doc);
        WalkAndClean(doc.DocumentNode);

        return doc.DocumentNode.InnerHtml;
    }

    static void RemoveCommentsAndDocType(HtmlDocument doc)
    {
        var nodes = doc.DocumentNode.SelectNodes("//comment()") ?? Enumerable.Empty<HtmlNode>();
        foreach (var n in nodes.ToList()) n.Remove();
        if (doc.DocumentNode.ChildNodes.Count > 0)
        {
            var first = doc.DocumentNode.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Document);
        }
    }

    static void RemoveDangerousNodes(HtmlDocument doc)
    {
        foreach (var name in DangerousTags)
        {
            var nodes = doc.DocumentNode.SelectNodes("//" + name);
            if (nodes == null) continue;
            foreach (var n in nodes.ToList()) n.Remove();
        }
    }

    static void WalkAndClean(HtmlNode root)
    {
        var nodes = root.SelectNodes(".//*") ?? Enumerable.Empty<HtmlNode>();
        foreach (var node in nodes.ToList())
        {
            if (node.NodeType != HtmlNodeType.Element) continue;

            if (!AllowedTags.Contains(node.Name))
            {
                var replacement = HtmlNode.CreateNode(node.InnerHtml ?? string.Empty);
                node.ParentNode.ReplaceChild(replacement, node);
                continue;
            }

            CleanAttributes(node);
            if (node.Name.Equals("a", StringComparison.OrdinalIgnoreCase)) FixLinkRel(node);
        }
    }

    static void CleanAttributes(HtmlNode node)
    {
        var keep = new List<HtmlAttribute>();
        foreach (var attr in node.Attributes.ToList())
        {
            if (OnAttrRegex.IsMatch(attr.Name)) { node.Attributes.Remove(attr); continue; }
            if (attr.Name.Equals("style", StringComparison.OrdinalIgnoreCase)) { node.Attributes.Remove(attr); continue; }

            var allowedForTag = AllowedAttrs.ContainsKey(node.Name) && AllowedAttrs[node.Name].Contains(attr.Name);
            var allowedGlobal = AllowedAttrs["*"].Contains(attr.Name) || DataAttrWildcard.IsMatch(attr.Name);
            if (!allowedForTag && !allowedGlobal) { node.Attributes.Remove(attr); continue; }

            if ((attr.Name.Equals("href", StringComparison.OrdinalIgnoreCase) || attr.Name.Equals("src", StringComparison.OrdinalIgnoreCase)))
            {
                if (!IsSafeUrl(attr.Value)) { node.Attributes.Remove(attr); continue; }
            }

            if (attr.Name.Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                var v = (attr.Value ?? "").Trim();
                if (v != "_blank" && v != "_self") attr.Value = "_self";
            }

            if (attr.Name.Equals("class", StringComparison.OrdinalIgnoreCase))
            {
                var v = (attr.Value ?? "").Trim();
                if (CssExpressionRegex.IsMatch(v) || UrlJsRegex.IsMatch(v)) { node.Attributes.Remove(attr); continue; }
            }

            keep.Add(attr);
        }

        node.Attributes.RemoveAll();
        foreach (var k in keep) node.Attributes.Add(k);
    }

    static void FixLinkRel(HtmlNode node)
    {
        var target = node.GetAttributeValue("target", "").Trim();
        if (target == "_blank")
        {
            var rel = node.GetAttributeValue("rel", "");
            var parts = new HashSet<string>(rel.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
            parts.Add("noopener");
            parts.Add("noreferrer");
            node.SetAttributeValue("rel", string.Join(" ", parts));
        }
    }

    static bool IsSafeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.StartsWith("#")) return true;
        if (value.StartsWith("//")) return false;
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return DataImageRegex.IsMatch(value);
        if (Uri.TryCreate(value, UriKind.Relative, out _)) return true;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return AllowedSchemes.Contains(uri.Scheme);
        return false;
    }
}