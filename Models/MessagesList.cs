using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MsgGenerator.Models
{
    public class MessagesList
    {
        public MessagesList(string customerid, string cardtype, string mobile, string smstext)
        {
            this.CustomerID = customerid;
            this.CardType = cardtype;
            this.Mobile = mobile;
            this.SMSText = smstext;
        }

        public string CustomerID { set; get; }
        public string CardType { set; get; }
        public string Mobile { set; get; }
        public string SMSText { set; get; }
    }
}
