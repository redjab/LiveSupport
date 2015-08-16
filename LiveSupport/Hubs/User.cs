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

        public static bool operator ==(User obj1, User obj2)
        {
            return obj1.UserName == obj2.UserName;
        }

        public static bool operator !=(User obj1, User obj2)
        {
            return obj1.UserName != obj2.UserName;
        }
    }
}