﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramPasswordBot.DTO
{
    internal class PasswordDTO
    {
        public int id {  get; set; }
        public string telegramUserId { get; set; }
        public int serviceId { get; set; }
        public string login { get; set; }
        public string decryptedPassword { get; set; }
        public DateTime createTime { get; set; }
    }
}
