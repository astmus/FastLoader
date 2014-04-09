using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FastLoader.Classes;

namespace FastLoader.Extensions
{
	public static class WebExtensions
	{
		static char[] _invalidChars;
		static WebExtensions()
		{
			_invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Concat(new []{'#'}).ToArray();
		}

		public static WebItem RemoveQueryParams(this Uri uri, params string[] keys)
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
			return new WebItem(b.Uri);
		}

		

	}
}
