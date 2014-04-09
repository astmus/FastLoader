using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Classes
{
	public class WebItem : Uri
	{
		const string START_PAGE = "storagefilestart.html";

		public WebItem(string uriString, UriKind uriKind)
			: base(uriString, uriKind)
		{

		}

		public WebItem(Uri uri)
			: base(uri.OriginalString, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative)
		{

		}


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
	}
}
