using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Data
{
	public class HistoryItem
	{
		public Uri Link { get; set; }
		public DateTime TimeOpening { get; set; }
		public Uri Favicon { get; set; }
		public String Title { get; set; }

		public HistoryItem()
		{

		}

		public HistoryItem(Uri uri)
		{
			Link = uri;
			TimeOpening = DateTime.Now;
			Favicon = new Uri (uri.Authority + "/favicon.ico");
		}

		public override string ToString()
		{
			return Title+" "+TimeOpening.ToString("yyyy.MM.dd HH:mm:ss");
		}
	}
}
