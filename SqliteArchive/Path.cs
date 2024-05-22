// Copyright (c) Max Kagamine
// Licensed under the Apache License, Version 2.0

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SqliteArchive;

/// <summary>
/// Represents an absolute Unix path, with helpers for manipulating the path.
/// </summary>
public class Path : IParsable<Path>, IEnumerable<string>, IEquatable<Path?>
{
    public static readonly Path Root = new(Array.Empty<string>());

    private readonly string[] segments;

    /// <summary>
    /// Creates a new <see cref="Path"/>.
    /// </summary>
    /// <param name="path">An absolute path, or path relative to root.</param>
    public Path(string path) : this(Root, path)
    { }

    /// <inheritdoc cref="Path(Path, string)"/>
    public Path(string baseDirectory, string relativePath) : this(new Path(baseDirectory), relativePath)
    { }

    /// <summary>
    /// Creates a new <see cref="Path"/> by resolving a relative path against <paramref name="baseDirectory"/>, similar
    /// to <see cref="Uri(Uri, string?)"/> with <c>baseUri</c> having a trailing slash.
    /// </summary>
    /// <param name="baseDirectory">The base path from which to start. Defaults to <see cref="Root"/> if <see
    /// langword="null"/>.</param>
    /// <param name="relativePath">An absolute path, or path relative to <paramref name="baseDirectory"/>.</param>
    public Path(Path? baseDirectory, string relativePath)
    {
        baseDirectory ??= Root;
        string[] relativePathSegments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (relativePath.StartsWith('/'))
        {
            segments = relativePathSegments;
        }
        else
        {
            List<string> segments = [.. baseDirectory.segments];

            foreach (string segment in relativePathSegments)
            {
                if (segment is "..")
                {
                    // Linux ignores extra ..'s once hitting root
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                }
                else if (segment is not ".")
                {
                    segments.Add(segment);
                }
            }

            this.segments = [.. segments];
        }
    }

    private Path(string[] segments)
    {
        this.segments = segments;
    }

    /// <summary>
    /// Whether this path is equal to <see cref="Root"/>.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Parent), nameof(BaseName))]
    public bool IsRoot => segments.Length == 0;

    /// <summary>
    /// Gets the parent directory of the current path, or <see langword="null"/> if root.
    /// </summary>
    public Path? Parent => IsRoot ? null : new(segments[..^1]);

    /// <summary>
    /// Gets the last segment in the path, or <see langword="null"/> if root.
    /// </summary>
    public string? BaseName => IsRoot ? null : segments[^1];

    public override string ToString() => $"/{string.Join('/', segments)}";

    /// <inheritdoc cref="ToString()"/>
    /// <param name="trailingSlash">Whether to append a trailing slash. (Root is always "/".)</param>
    public string ToString(bool trailingSlash) => trailingSlash && !IsRoot ? ToString() + "/" : ToString();

    public static Path Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Path result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        result = new(s);
        return true;
    }

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)segments).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => segments.GetEnumerator();

    public override bool Equals(object? obj) => Equals(obj as Path);

    public bool Equals(Path? other) => other is not null && segments.SequenceEqual(other.segments);

    public override int GetHashCode() => ToString().GetHashCode();

    public static bool operator ==(Path? left, Path? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Path? left, Path? right) => !(left == right);

    /// <inheritdoc cref="Path(Path?, string)"/>
    public static Path operator +(Path? baseDirectory, string relativePath) => new(baseDirectory ?? Root, relativePath);
}
