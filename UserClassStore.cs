﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    internal class UserClassStore
    {
        public static ConcurrentDictionary<long, UserClass> UserClasses = new ConcurrentDictionary<long, UserClass>();
    }
}

//gint1k
