using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSPToolkit.Encodings;
using System.IO;

namespace FastLoader
{
	static class Utils
	{
		private static string format = "0.00";
		public static string ConvertCountBytesToString(long value)
		{
			double res = (double)value;

			if (res / 1024 < 1)
				return (res).ToString(format) + " Byte";
			res /= 1024;

			if (res / 1024 < 1)
				return (res).ToString(format) + " KByte";
			res /= 1024;

			if (res / 1024 < 1)
				return (res).ToString(format) + " MByte";

			return (res / 1024).ToString(format) + " GByte";
		}

		public static Stream CopyAndClose(Stream inputStream)
		{
			const int readSize = 256;
			byte[] buffer = new byte[readSize];
			MemoryStream ms = new MemoryStream();

			int count = inputStream.Read(buffer, 0, readSize);
			while (count > 0)
			{
				ms.Write(buffer, 0, count);
				count = inputStream.Read(buffer, 0, readSize);
			}
			ms.Position = 0;
			inputStream.Close();
			return ms;
		}

		const string WINDOWS_1250 = "windows-1250";
		const string WINDOWS_1251 = "windows-1251";
		const string WINDOWS_1252 = "windows-1252";
		const string WINDOWS_1253 = "windows-1253";
		const string WINDOWS_1254 = "windows-1254";
		const string WINDOWS_1255 = "windows-1255";
		const string WINDOWS_1256 = "windows-1256";
		const string WINDOWS_1257 = "windows-1257";
		const string WINDOWS_1258 = "windows-1258";
		
		public static Encoding GetEncodingByString(string charset)
		{
			switch (charset)
			{
				case WINDOWS_1250:
					break;
				case WINDOWS_1251:
					return new Windows1251Encoding();
				case WINDOWS_1252:
					break;
				case WINDOWS_1253:
					break;
				case WINDOWS_1254:
					break;
				case WINDOWS_1255:
					break;
				case WINDOWS_1256:
					break;
				case WINDOWS_1257:
					break;
				case WINDOWS_1258:
					break;
			}
			return null;
		}
	}
}
