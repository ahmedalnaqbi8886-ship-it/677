using System;

namespace EaxReverbEngine
{
	// نظام الترخيص والاتصال بالسيرفر انشال بالكامل بطلب صريح - البرنامج صار مجاني ومفتوح
	// بدون أي فحص أو اتصال شبكة.
	internal static class LicenseGate
	{
		public static bool Ensure()
		{
			return true;
		}

		public static void Watch(Action onExpired)
		{
			// ولا شي - ما فيه فحص دوري ولا اتصال بعد الآن.
		}
	}
}
