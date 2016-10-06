using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 正反问+值已定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input">内存16G的iphone5s有没有？</param>
    /// <param name="info">品牌：iphone5s； 内存：16G</param>
    /// <returns></returns>
    public class NEWSTP5 : ISTP
    {
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Extracting.ValidInfo info)
        {
            //用户希望系统根据当前句的语义和之前的收集的信息立即推荐一款手机 
            if (Regex.IsMatch(input, @"推荐"))
            {
                userSession.liveTable.Candidates = Inquiry.Screening(userSession.liveTable.Parameter, info);
                if (userSession.liveTable.Candidates.Count == 0)
                {
                    return Answering.NotFound(userSession, info);
                }
                else
                {
                    // 目前能对多个值已定的属性进行手机推荐
                    userSession.liveTable.TalkingAbout = userSession.liveTable.Candidates[0];
                    userSession.liveTable.Candidates.Remove(userSession.liveTable.TalkingAbout);
                    userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                    return userSession.liveTable.TalkingAbout.Brand + userSession.liveTable.TalkingAbout.Model + "这款不错，你可以了解一下。";
                }
            }

            bool isDirect = false; //是不是直接问一款准确的手机型号
            bool isPropertyExist = false; //询问的属性是否已经存在于livetable中
            List<Models.CellPhone> list = new List<Models.CellPhone>();
            foreach (var it in info.items)
            {
                if (String.IsNullOrEmpty(info.conflictFeedback) && it.name.Equals("型号"))
                {
                    if (it.items.Count > 0)
                        isDirect = true;
                }
                if (userSession.liveTable.Parameter[it.name] != null)
                {
                    isPropertyExist = true;
                }
            }
            if (!isDirect)
            {
                // 针对某一款手机
                if (userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.DEBUGGING &&
                    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.LEARNING &&
                    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.START &&
                    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.END)
                {
                    if (userSession.liveTable.TalkingAbout != null)
                    {
                        if (!isPropertyExist)
                        {
                            userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;
                            string answer = "";
                            for (int i = 0; i < info.items.Count; i++)
                            {
                                string value = userSession.liveTable.TalkingAbout.GetType().GetProperty
                                    (Code.GlobalHash.SemanticToEN[info.items[i].name].ToString()).GetValue(userSession.liveTable.TalkingAbout, null).ToString();
                                if (Regex.IsMatch(info.items[i].items[0].normalData, @"\d+\.*\d+,\d+\.*\d+"))
                                {
                                    string[] bounds = info.items[i].items[0].normalData.Split(new char[] { ',' });
                                    if (Convert.ToDouble(bounds[0]) <= Convert.ToDouble(value) &&
                                        Convert.ToDouble(value) <= Convert.ToDouble(bounds[1]))
                                    {
                                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, true) + info.items[i].items[0].rawData;
                                    }
                                    else
                                    {
                                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, false) + info.items[i].items[0].rawData;
                                    }
                                }
                                else
                                {
                                    if (value.IndexOf(info.items[i].items[0].normalData) != -1)
                                    {
                                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, true) + info.items[i].items[0].rawData;
                                    }
                                    else
                                    {
                                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, false) + info.items[i].items[0].rawData;
                                    }

                                }
                            }
                            if (answer[answer.Length - 1] == '|')
                                answer = answer.Substring(0, answer.Length - 1) + "。";
                            else
                                answer = answer.Substring(0, answer.Length) + "。";
                            return answer;
                        }
                        else
                        {
                            userSession.liveTable.TalkingAbout.Model = null;
                            List<string> ps = new List<string>();
                            List<Models.CellPhone> phonelist = Inquiry.Screening(userSession.liveTable.Parameter, info);
                            string answer = "";
                            if (phonelist.Count > 0)
                            {
                                userSession.liveTable.Candidates = phonelist;
                                answer = "是的，";
                                for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                                answer += Answering.Statement(phonelist[0], ps);
                                if (input.IndexOf("有") != -1)
                                {
                                    answer = answer.Replace("是", "有");
                                    answer = answer.Replace("这款", "");
                                }
                                else if (input.IndexOf("是") != -1)
                                {
                                    answer = answer.Replace("有", "是");
                                    answer = answer.Replace("这款", "");
                                }
                                if (phonelist.Count <= 3)
                                {
                                    userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                                    userSession.liveTable.TalkingAbout = phonelist[0];
                                    answer += Answering.Recommand(phonelist[0]);
                                }

                                return answer;
                            }
                            else
                            {
                                return Answering.NotFound(userSession, info);
                            }
                        }
                    }
                    else
                    {
                        List<string> ps = new List<string>();
                        List<Models.CellPhone> phonelist = Inquiry.Screening(userSession.liveTable.Parameter, info);
                        string answer = "";
                        if (phonelist.Count > 0)
                        {
                            userSession.liveTable.Candidates = phonelist;
                            answer = "是的，";
                            for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                            answer += Answering.Statement(phonelist[0], ps);
                            if (input.IndexOf("有") != -1)
                            {
                                answer = answer.Replace("是", "有");
                                answer = answer.Replace("这款", "");
                            }
                            else if (input.IndexOf("是") != -1)
                            {
                                answer = answer.Replace("有", "是");
                                answer = answer.Replace("这款", "");
                            }


                            foreach (string s in Code.GlobalHash.ImportantParameter)
                            {
                                if (!userSession.liveTable.Parameter.ContainsKey(s))
                                {
                                    userSession.liveTable.RelatedParameter = s;
                                    userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.ACTIVE;
                                    answer += Answering.ActiveLeading(s);
                                    break;
                                }
                           }
                           return answer;
                        }
                        else
                        {
                            return Answering.NotFound(userSession, info);
                        }
                    }
                }

            }
            else
            {
                string answer = "";
                List<string> ps = new List<string>();
                list = Inquiry.Screening(userSession.liveTable.Parameter, info);
                if (list.Count <= 0)
                {
                    if (info.items.Count < 2)
                    {
                        answer = "亲，不好意思，" + "没有" + info.items[0].items[0].rawData;
                        return answer;
                    }
                    else
                    {
                        answer = "亲，不好意思，" + "我们还没收录您提到的这个型号，我们正在努力更新数据~~";
                        return answer;
                    }
                }
                else
                {
                    for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                    return Answering.Statement(list[0], ps);
                }
            }
            return "";
        }
    }
}