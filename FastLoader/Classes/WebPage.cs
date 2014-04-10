using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FastLoader.Classes
{
	public class WebItem : Uri
	{
		const string START_PAGE = "storagefilestart.html";
		static char[] _invalidChars;

		static WebItem()
		{
			_invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Concat(new []{'#'}).ToArray();
		}

		public WebItem(string uriString, UriKind uriKind)
			: base(uriString, uriKind)
		{

		}

		public WebItem(Uri uri)
			: base(uri.OriginalString, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative)
		{

		}

		string _localHystoryFileName;
		public string LocalHystoryFileName
		{
			get{
				if (_localHystoryFileName == null)
				{
					StringBuilder b = new StringBuilder(this.OriginalString);

					foreach (char c in _invalidChars)
						b.Replace(c, ' ');
                    
					b.Insert(0, "storagefile");
					b.Replace(" ", "");
					b.Replace(".", "");
					if (b.Length > 150)
						b.Remove(150, b.Length - 150);
					b.Append(".html");
					_localHystoryFileName = b.ToString();
				}
				return _localHystoryFileName;
			}
			
		}

        WebItem _localHistoryItem;
		public WebItem LocalHystoryUri
		{
			get
			{
				if (!this.IsAbsoluteUri) return this;
                if (_localHistoryItem == null)
                {
                    string res = LocalHystoryFileName;
                    return new WebItem(res, UriKind.Relative);
                }
                return _localHistoryItem;
			}
		}

#region Static

		static WebItem _startPage;
		public static WebItem StartPage
		{
			get
			{
				return _startPage ?? (_startPage = new WebItem(START_PAGE, UriKind.Relative));
			}
		}

		static WebItem _googlePage;
		public static WebItem GooglePage
		{
			get
			{
				return _googlePage ?? (_googlePage = new WebItem("https://www.google.com", UriKind.Absolute));
			}
		}
#endregion
	}
}
