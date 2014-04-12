using FastLoader.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Data
{
	public class HistoryItemsByDate : List<ItemsGroup<HistoryItem>>
	{
		public HistoryItemsByDate()
		{
			Dictionary<DateTime, List<HistoryItem>> dates = (from item in FSDBManager.Instance.History
															 group item by item.OpenTime.Date).ToDictionary(g => g.Key,
															 g => g.OrderByDescending(x => x.OpenTime).ToList());
			
			foreach (DateTime dt in dates.Keys)
			{
				ItemsGroup<HistoryItem> g = new ItemsGroup<HistoryItem>(dt.ToString("dd MMMM yyyy"));
				g.AddRange(dates[dt]);
				this.Add(g);
			}
			
			//dates.Sort();
			//dates.Reverse();
		}
	}
}
