using System;

namespace EaxReverbEngine
{
	internal enum LicState
	{
		Ok,
		NoKey,
		BadKey,
		WrongHwid,
		Expired,
		Revoked,
		Offline,
		ClockTampered
	}

	// نظام الترخيص والاتصال بالسيرفر (n7-r67.onrender.com) انشال بالكامل بطلب صريح:
	// ما فيه قراءة HWID، ما فيه ملفات cache/stamp، وما فيه أي اتصال شبكة من هذا الكلاس نهائيًا.
	// خليناه بس يرجع "مفعّل دايمًا" عشان أي كود ثاني يستدعي License.* ما ينكسر.
	internal static class License
	{
		public static string Hwid()
		{
			return "";
		}

		public static string HwidPretty()
		{
			return "";
		}

		public static LicState Validate(string key)
		{
			return LicState.Ok;
		}

		public static string Load()
		{
			return "";
		}

		public static void Store(string key)
		{
		}

		public static LicState Check()
		{
			return LicState.Ok;
		}

		public static string Describe(LicState s)
		{
			return "Active - Free";
		}

		public static string PlanName = "Free";

		public static DateTime Expiry = DateTime.MaxValue;
	}
}
