using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Classes
{
	public class HistoryItem : WebItem
	{
		public Uri Link { get; set; }
		public DateTime TimeOpening { get; set; }
		public Uri Favicon { get; set; }
		public String Title { get; set; }

		public HistoryItem(WebItem item) : base (item.OriginalString, item.IsAbsoluteUri? UriKind.Absolute : UriKind.Relative)
		{

		}

		public HistoryItem() : base("",UriKind.Relative)
		{

		}

		public override string ToString()
		{
			return Title+" "+TimeOpening.ToString("yyyy.MM.dd HH:mm:ss");
		}
	}
}
