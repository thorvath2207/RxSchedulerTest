using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveThreadSucks
{
    public class Event
    {
        public DateTime AddTime { get; set; }
        public string Content { get; set; }
        public string ThreadId { get; set; }
    }
}
