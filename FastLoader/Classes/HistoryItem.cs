using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Classes
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

		public override string ToString()
		{
			return Title+" "+TimeOpening.ToString("yyyy.MM.dd HH:mm:ss");
		}
	}
}
