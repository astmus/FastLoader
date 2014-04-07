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
			List<HistoryItem> items = new List<HistoryItem>();
			DateTime d = DateTime.Now;
			Random r = new Random();
			for (int i = 0; i < 40; i++)
			{
				HistoryItem h = new HistoryItem();
				h.Title = "Title" + i.ToString();
				d = d.AddHours(8);
				h.TimeOpening = d;
				items.Insert(r.Next(0,items.Count),h);
			}

			Dictionary<DateTime, List<HistoryItem>> dates = (from item in items group item by item.TimeOpening.Date into custGroup
															 orderby custGroup.Key.Date descending select custGroup).ToDictionary(g => g.Key,
															 g => g.OrderByDescending(x=>x.TimeOpening.Date).ToList());
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
