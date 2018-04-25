using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TeamsTalentMgmtApp.DataModel
{
    public class InterviewRequest
    {
        public string CandidateName { get; set; }
        public string ReqId { get; set; }
        public string PositionTitle { get; set; }
        public bool Remote { get; set; }
        public DateTime Date { get; set; }
    }
}