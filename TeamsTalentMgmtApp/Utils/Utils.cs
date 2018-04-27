﻿using Bogus;
using System;
using System.Collections.Generic;
using TeamsTalentMgmtApp.DataModel;
using static Bogus.DataSets.Name;

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
        const int numPeople = 5;

        public List<Candidate> GetTopCandidates(string reqId)
        {
            List<Candidate> resp = new List<Candidate>();

            for (int i = 0; i < numPeople; i++)
            {
                Candidate c = GenerateCandidate(i + 1);
                c.ReqId = reqId;
                resp.Add(c);
            }
            return resp;
        }

        public Candidate GetCandidateByName(string name)
        {
            Candidate c = GenerateCandidate(1);
            c.Name = name;
            return c;
        }

        private Candidate GenerateCandidate(int index)
        {
            Random r = new Random();
            var faker = new Faker();
            Person p = faker.Person;

            Candidate c = new Candidate()
            {
                Name = $"{p.FirstName} {p.LastName}",
                CurrentRole = faker.PickRandom(Constants.Titles),
                Hires = r.Next() % 4,
                NoHires = r.Next() % 4,
                Stage = faker.PickRandom(Constants.Stages),
                ProfilePicture = Utils.GetRootUrl() + $"/images/" + p.Gender.ToString().ToLower() + $"/candidate_{index}.png",
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