using System.Reflection;
using RimWorld;

namespace GravshiptoSpaceship;

public static class CompAffectedByFacilitiesExtensions
{
	public static void ForceRelinkAll(this CompAffectedByFacilities comp)
	{
		typeof(CompAffectedByFacilities).GetMethod("RelinkAll", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(comp, null);
	}
}
