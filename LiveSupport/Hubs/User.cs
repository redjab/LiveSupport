using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LiveSupport.Hubs
{
    public class User
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string ConnectionID { get; set; }
    }
}