using System;
using System.Text;

namespace EaxReverbEngine
{
	// Token: 0x02000013 RID: 19
	public static class SelfTest
	{
		// Token: 0x06000086 RID: 134 RVA: 0x00006A08 File Offset: 0x00004C08
		public static string Run(string vstPath, int rate)
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			VstPlugin vstPlugin = new VstPlugin();
			try
			{
				vstPlugin.Load(vstPath, rate, 4160);
				stringBuilder.AppendLine("loaded: " + vstPath);
				stringBuilder.AppendFormat("progs={0} params={1} in={2} out={3} uid=0x{4:X8} ver={5}", new object[]
				{
					vstPlugin.ProgramCount,
					vstPlugin.ParameterCount,
					vstPlugin.InputCount,
					vstPlugin.OutputCount,
					vstPlugin.UniqueId,
					vstPlugin.Version
				}).AppendLine();
				if (vstPlugin.ProgramCount != 15)
				{
					stringBuilder.AppendLine("FAIL: program count != 15");
					flag = false;
				}
				if (vstPlugin.ParameterCount != 12)
				{
					stringBuilder.AppendLine("FAIL: parameter count != 12");
					flag = false;
				}
				if (vstPlugin.InputCount != 2 || vstPlugin.OutputCount != 2)
				{
					stringBuilder.AppendLine("FAIL: channels != 2/2");
					flag = false;
				}
				int programCount = vstPlugin.ProgramCount;
				for (int i = 0; i < programCount; i++)
				{
					stringBuilder.AppendFormat("prog[{0}]=\"{1}\"", i, vstPlugin.GetProgramName(i)).AppendLine();
				}
				for (int i = 0; i < programCount; i++)
				{
					vstPlugin.SetProgram(i);
				}
				vstPlugin.SetProgram(6);
				vstPlugin.SetParameter(1, 0.7f);
				vstPlugin.SetParameter(2, 1f);
				float parameter = vstPlugin.GetParameter(1);
				stringBuilder.AppendFormat("param1 after set(0.7) read back = {0:F4}", parameter).AppendLine();
				if (Math.Abs(parameter - 0.7f) > 0.01f)
				{
					stringBuilder.AppendLine("FAIL: param roundtrip");
					flag = false;
				}
				float parameter2 = vstPlugin.GetParameter(2);
				stringBuilder.AppendFormat("param2 read back = {0:F2}", parameter2).AppendLine();
				if (parameter2 < 0.5f)
				{
					stringBuilder.AppendLine("FAIL: enable param");
					flag = false;
				}
				int num = 4096;
				float[] array = new float[num];
				float[] array2 = new float[num];
				float[] array3 = new float[num];
				float[] array4 = new float[num];
				int num2 = rate * 4;
				int num3 = 100;
				long num4 = -1L;
				double num5 = 0.0;
				float num6 = 0f;
				int num7 = 0;
				int num8;
				for (int j = 0; j < num2; j += num8)
				{
					num8 = Math.Min(num, num2 - j);
					Array.Clear(array, 0, num8);
					Array.Clear(array2, 0, num8);
					for (int k = 0; k < num8; k++)
					{
						if (j + k == num3)
						{
							array[k] = 1f;
							array2[k] = 1f;
						}
					}
					vstPlugin.Process(array, array2, array3, array4, num8);
					for (int k = 0; k < num8; k++)
					{
						int num9 = j + k;
						float num10 = Math.Abs(array3[k]);
						if (Math.Abs(array4[k]) > num10)
						{
							num10 = Math.Abs(array4[k]);
						}
						if (num10 > num6)
						{
							num6 = num10;
							num7 = (int)((double)(num9 - num3) * 1000.0 / (double)rate);
						}
						if (num9 > num3 + rate / 50)
						{
							if (num10 > 1E-06f)
							{
								num4 = (long)num9;
							}
							num5 += (double)array3[k] * (double)array3[k] + (double)array4[k] * (double)array4[k];
						}
					}
				}
				int num11 = (num4 >= 0L) ? ((int)((double)(num4 - (long)num3) * 1000.0 / (double)rate)) : 0;
				stringBuilder.AppendFormat("impulse: peak={0:F4} at {1}ms, reverb tail={2}ms, energy(after 20ms)={3:E3}", new object[]
				{
					num6,
					num7,
					num11,
					num5
				}).AppendLine();
				if (num6 < 0.1f)
				{
					stringBuilder.AppendLine("FAIL: no output from impulse");
					flag = false;
				}
				if (num11 < 100 || num5 <= 0.0)
				{
					stringBuilder.AppendLine("FAIL: no reverb tail");
					flag = false;
				}
				vstPlugin.SetParameter(2, 0f);
				float num12 = 0f;
				float num13 = (float)(1382.300767579509 / (double)rate);
				double num14 = 0.0;
				for (int l = 0; l < 4; l++)
				{
					for (int k = 0; k < num; k++)
					{
						float num15 = (float)Math.Sin((double)num12);
						num12 += num13;
						array[k] = num15;
						array2[k] = num15;
					}
					vstPlugin.Process(array, array2, array3, array4, num);
					for (int k = 0; k < num; k++)
					{
						double num16 = (double)Math.Abs(array[k] - array3[k]);
						if (num16 > num14)
						{
							num14 = num16;
						}
						num16 = (double)Math.Abs(array2[k] - array4[k]);
						if (num16 > num14)
						{
							num14 = num16;
						}
					}
				}
				stringBuilder.AppendFormat("bypass (enabled=0): max |in-out| = {0:E3}", num14).AppendLine();
				if (num14 > 0.001)
				{
					stringBuilder.AppendLine("FAIL: disabled mode is not dry passthrough");
					flag = false;
				}
				vstPlugin.SetParameter(2, 1f);
				Array.Clear(array, 0, num);
				Array.Clear(array2, 0, num);
				array[0] = 1f;
				array2[0] = 1f;
				double num17 = 0.0;
				for (int l = 0; l < 12; l++)
				{
					if (l > 0)
					{
						Array.Clear(array, 0, num);
						Array.Clear(array2, 0, num);
					}
					vstPlugin.Process(array, array2, array3, array4, num);
					if (l > 1)
					{
						for (int k = 0; k < num; k++)
						{
							num17 += (double)array3[k] * (double)array3[k] + (double)array4[k] * (double)array4[k];
						}
					}
				}
				stringBuilder.AppendFormat("re-enable impulse energy = {0:E3}", num17).AppendLine();
				if (num17 <= 0.0)
				{
					stringBuilder.AppendLine("FAIL: reverb dead after re-enable");
					flag = false;
				}
			}
			catch (Exception ex)
			{
				stringBuilder.AppendLine("FAIL: exception " + ex);
				flag = false;
			}
			finally
			{
				try
				{
					vstPlugin.Dispose();
				}
				catch (Exception ex)
				{
					stringBuilder.AppendLine("dispose: " + ex.Message);
				}
			}
			stringBuilder.AppendLine(flag ? "SELFTEST PASS" : "SELFTEST FAIL");
			return stringBuilder.ToString();
		}
	}
}
