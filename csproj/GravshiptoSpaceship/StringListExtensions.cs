using System;
using System.Collections.Generic;

namespace GravshiptoSpaceship;

public static class StringListExtensions
{
	public static string ToLineList(this IEnumerable<string> lines)
	{
		return string.Join("\n", lines).TrimEnd(Array.Empty<char>());
	}
}
