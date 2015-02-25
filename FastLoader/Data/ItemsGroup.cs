using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.Data
{
	public class ItemsGroup<T> : ObservableCollection<T>
	{
		public ItemsGroup(DateTime groupDate, IList<T> items) : base(items)
        {
			Key = groupDate.ToString("dd MMMM yyyy");
			GroupDate = groupDate;
        }

		public DateTime GroupDate { get; set; }

        public string Key { get; set; }

        public bool HasItems { get { return Count > 0; } }
	}
}
