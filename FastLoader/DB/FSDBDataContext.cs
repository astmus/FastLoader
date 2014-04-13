using FastLoader.Data;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastLoader.Interfaces;

namespace FastLoader.DB
{
	public class FSDBManager : DataContext
	{
		static FSDBManager _dbManager;
		public static FSDBManager Instance
		{
			get { return _dbManager ?? (_dbManager = new FSDBManager()); }
			set { _dbManager = value; }
		}

		public FSDBManager(string connection = "Data source=isostore:/hystory.sdf") :
            base(connection)
        {
            if (this.DatabaseExists() == false)
                this.CreateDatabase();
        }


		public List<ItemsGroup<T>> GetSortedItems<T>() where T : class, IWebItem
		{
			List<ItemsGroup<T>> res = new List<ItemsGroup<T>>();
			Dictionary<DateTime, List<T>> dates = (from item in FSDBManager.Instance.GetTable<T>()
															group item by item.OpenTime.Date).ToDictionary(g => g.Key,
															 g => g.OrderByDescending(x => x.OpenTime).ToList());
			foreach (DateTime dt in dates.Keys)
			{
				ItemsGroup<T> g = new ItemsGroup<T>(dt.ToString("dd MMMM yyyy"));
				g.AddRange(dates[dt]);
				res.Insert(0, g);
			}
			return res;
		}		
		
		public System.Data.Linq.Table<CachedItem> Cache
		{
			get
			{
				return this.GetTable<CachedItem>();
			}
		}

		public System.Data.Linq.Table<HistoryItem> History
		{
			get
			{
				return this.GetTable<HistoryItem>();
			}
		}
	}
}
