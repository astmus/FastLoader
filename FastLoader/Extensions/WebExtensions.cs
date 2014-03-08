using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Extensions
{
	public static class WebExtensions
	{
		static char[] _invalidChars;
		static WebExtensions()
		{
			_invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
		}

		public static Uri RemoveQueryParams(this Uri uri, params string[] keys)
		{
			UriBuilder b = new UriBuilder(uri);
			int position;
			Dictionary<string, string> queryParams = null;
			if ((position = b.Path.IndexOf('&')) != -1)
			{
				queryParams = b.Path.Substring(position).Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries).Select(par => par.Split('=')).ToDictionary(x => x[0], x => x[1]);
				b.Path = b.Path.Remove(position);

				foreach (string key in keys)
					if (queryParams.ContainsKey(key))
						queryParams.Remove(key);

				b.Query = string.Join("&", queryParams.Select(x => x.Key + "=" + x.Value).ToArray());
			}
			return b.Uri;
		}

		public static string GetLocalHystoryFileName(this Uri uri)
		{
			StringBuilder b = new StringBuilder(uri.OriginalString);

			foreach (char c in _invalidChars)
				b.Replace(c, ' ');

			//b.Remove(0, 11);
			b.Insert(0, "storagefile");
			b.Replace(" ", "");
			b.Replace(".", "");
			b.Append(".html");
			return b.Length < 150 ? b.ToString() : b.ToString(0, 150);
		}

		public static Uri AsLocalHystoryUri(this Uri uri)
		{
			StringBuilder b = new StringBuilder(uri.OriginalString);

			foreach (char c in _invalidChars)
				b.Replace(c, ' ');

			//b.Remove(0, 11);
			b.Insert(0, "storagefile");
			b.Replace(" ", "");
			b.Replace(".", "");
			b.Append(".html");
			return new Uri(b.Length < 150 ? b.ToString() : b.ToString(0, 150),UriKind.Relative);
		}


		public static int WordCount(this String str)
		{
			return str.Split(new char[] { ' ', '.', '?' },
							 StringSplitOptions.RemoveEmptyEntries).Length;
		}
	}
}
