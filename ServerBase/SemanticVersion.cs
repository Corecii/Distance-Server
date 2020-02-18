using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Semantic version struct with comparison operators
// Includes support for a fork code:
//  if a developer forks the project and makes different changes for the same version number,
//  the fork code will indicate that the branches may have incompatible features even if the major, minor, and patch numbers are the same.

public class SemanticVersionParseException : Exception
{
    public SemanticVersionParseException(string message) : base(message) { }
}
public struct SemanticVersion
{
    public static SemanticVersion Empty = new SemanticVersion();

    public bool empty
    {
        get
        {
            return string.IsNullOrEmpty(forkCode)
                && major == 0
                && minor == 0
                && patch == 0;
        }
    }
    public readonly string forkCode;
    public readonly int major;
    public readonly int minor;
    public readonly int patch;

    public SemanticVersion(string forkCode, int major, int minor, int patch)
    {
        this.forkCode = forkCode;
        this.major = major;
        this.minor = minor;
        this.patch = patch;
    }

    public SemanticVersion(int major, int minor, int patch)
    {
        this.forkCode = "";
        this.major = major;
        this.minor = minor;
        this.patch = patch;
    }

    public SemanticVersion(string versionString)
    {
        var match = Regex.Match(versionString, @"^(\D*)\.?(\d+)\.(\d+)\.(\d+)$");
        if (!match.Success)
            throw new SemanticVersionParseException("Bad version string format");
        forkCode = match.Groups[1].Value;
        if (!int.TryParse(match.Groups[2].Value, out major))
            throw new SemanticVersionParseException("Bad int format for major");
        if (!int.TryParse(match.Groups[3].Value, out minor))
            throw new SemanticVersionParseException("Bad int format for minor");
        if (!int.TryParse(match.Groups[4].Value, out patch))
            throw new SemanticVersionParseException("Bad int format for patch");
    }

    public static bool TryParse(string input, out SemanticVersion version)
    {
        try
        {
            version = new SemanticVersion(input);
            return true;
        }
        catch (SemanticVersionParseException)
        {
            version = SemanticVersion.Empty;
            return false;
        }
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(forkCode))
        {
            return $"{major}.{minor}.{patch}";
        }
        else
        {
            return $"{forkCode}.{major}.{minor}.{patch}";
        }
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        if (left.forkCode != right.forkCode)
            return false;
        if (left.major < right.major)
            return true;
        else if (left.major == right.major)
        {
            if (left.minor < right.minor)
                return true;
            else if (left.minor == right.minor)
                return left.patch < right.patch;
        }
        return false;
    }
    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        if (left.forkCode != right.forkCode)
            return false;
        return !(left < right);
    }
    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        if (left.forkCode != right.forkCode)
            return false;
        if (left.major > right.major)
            return true;
        else if (left.major == right.major)
        {
            if (left.minor > right.minor)
                return true;
            else if (left.minor == right.minor)
                return left.patch > right.patch;
        }
        return false;
    }
    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        if (left.forkCode != right.forkCode)
            return false;
        return !(left > right);
    }
    public static bool operator ==(SemanticVersion left, SemanticVersion right)
    {
        return left.forkCode == right.forkCode
            && left.major == right.major
            && left.minor == right.minor
            && left.patch == right.patch;
    }
    public static bool operator !=(SemanticVersion left, SemanticVersion right)
    {
        return !(left == right);
    }
    public override bool Equals(object o)
    {
        if (!(o is SemanticVersion))
            return false;
        return (SemanticVersion)o == this;
    }
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
    public static int Comparison(SemanticVersion left, SemanticVersion right)
    {
        if (left.forkCode != right.forkCode)
        {
            return string.Compare(left.forkCode, right.forkCode);
        }
        return left < right ? -1 : left > right ? 1 : 0;
    }
}
