using Bogus;
using System;
using System.Collections.Generic;
using TeamsTalentMgmtApp.DataModel;

namespace TeamsTalentMgmtApp.Utils
{
    public static class Constants
    {
        public static List<string> Titles = new List<string>
        {
            "Graphics Artist",
            "Senior Content Writer",
            "Senior Program Manager",
            "Software Developer II",
            "Principal Product Manager",
            "Marketing Manager",
            "Development Lead",
            "UX Designer"
        };

        public static List<string> Stages = new List<string>
        {
            "Applied",
            "Interviewing",
            "Pending",
            "Offered"
        };
    }

    public class OpenPositionsDataController
    {

        public List<OpenPosition> ListOpenPositions()
        {
            const int numPositions = 5;

            List<OpenPosition> resp = new List<OpenPosition>();

            for (int i = 0; i < numPositions; i++)
            {
                resp.Add(GeneratePosition());
            }
            return resp;
        }

        public OpenPosition GetPositionForReqId(string reqId)
        {
            OpenPosition pos = GeneratePosition();
            pos.ReqId = reqId;
            return pos;
        }

        private OpenPosition GeneratePosition()
        {
            Random r = new Random();
            var faker = new Faker();

            OpenPosition p = new OpenPosition()
            {
                Title = faker.PickRandom(Constants.Titles),
                DaysOpen = r.Next() % 10,
                HiringManager = $"{faker.Name.FirstName()} {faker.Name.LastName()}",
                Applicants = r.Next() % 5,
                ReqId = Guid.NewGuid().ToString().Split('-')[0].ToUpper()
            };

            return p;
        }
    }

    public class CandidatesDataController
    {
        public List<Candidate> GetTopCandidates(string reqId)
        {
            const int numCandidates = 3;

            List<Candidate> resp = new List<Candidate>();

            for (int i = 0; i < numCandidates; i++)
            {
                Candidate c = GenerateCandidate();
                c.ReqId = reqId;
                c.ProfilePicture = Utils.GetRootUrl() + $"/images/candidate_{(i + 1)}.png";
                resp.Add(c);
            }
            return resp;
        }

        public Candidate GetCandidateByName(string name)
        {
            Candidate c = GenerateCandidate();
            c.Name = name;
            return c;
        }

        private Candidate GenerateCandidate()
        {
            Random r = new Random();
            var faker = new Faker();

            Candidate c = new Candidate()
            {
                Name = $"{faker.Name.FirstName()} {faker.Name.LastName()}",
                CurrentRole = faker.PickRandom(Constants.Titles),
                Hires = r.Next() % 3,
                NoHires = r.Next() % 3,
                Stage = faker.PickRandom(Constants.Stages),
                ProfilePicture = Utils.GetRootUrl() + $"/images/candidate_{(r.Next(1, 3))}.png",
                ReqId = Guid.NewGuid().ToString().Split('-')[0].ToUpper()
            };

            return c;
        }
    }

    public static class Utils {

        public static string GetRootUrl()
        {
            if (System.Web.HttpContext.Current.Request.Headers["x-original-host"] != null)
            {
                return "https://" + System.Web.HttpContext.Current.Request.Headers["x-original-host"];
            } else
            {
                return "https://" + System.Web.HttpContext.Current.Request.Url.Host;
            }
        }
    }

    public class TabContext
    {
        public string ChannelId { get; set; }
        public string CanvasUrl { get; set; }
    }

}