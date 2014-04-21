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
		public ItemsGroup(string category, IList<T> items) : base(items)
        {
            Key = category;
        }

        public string Key { get; set; }

        public bool HasItems { get { return Count > 0; } }
	}
}
