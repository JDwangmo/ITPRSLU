using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 选择问+值已定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public class NEWSTP7 : ISTP
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

            /* 从live-table的当前谈论机中，选取符合最合适的手机参数值进行回答
              * 这里提取到的信息并不需要更新到live-table里
              * */

            if (userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.DEBUGGING &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.LEARNING &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.START &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.END)
            {
                /* 可能用户会说：这款手机是水货还是行货？|这个价位是高还是低？.....
                 * 这可以从TalkingAbout找相应的描述返回
                 * */
                bool isDirect = false;
                foreach (var it in info.items)
                {
                    if (String.IsNullOrEmpty(info.conflictFeedback) && it.name.Equals("型号"))
                    {
                        if (it.items.Count > 0)
                            isDirect = true;
                        break;
                    }
                }
                if (!isDirect)
                {
                    if (info.items[0].items.Count > 0)
                    {
                        if (info.items.Count >= 1)
                        {
                            // 小米2s的系统是Androd 4.0吗？
                            Areas.Chat.Models.LiveTable liveTableTemp = new Areas.Chat.Models.LiveTable();
                            List<string> ps = new List<string>();
                            List<Models.CellPhone> phonelist = Inquiry.Screening(liveTableTemp.Parameter, info);
                            string answer = "";
                            if (phonelist.Count > 0)
                            {
                                answer = "是的，";
                            }
                            else
                            {
                                answer = "不好意思，" + "没有" + info.items[1].items[0];
                                Extracting.ValidInfo infoTemp = new Extracting.ValidInfo();
                                liveTableTemp.Parameter[info.items[0].name] = info.items[0].items[0];
                                phonelist = Inquiry.Screening(liveTableTemp.Parameter, infoTemp);
                            }
                            for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                            answer += Answering.Statement(phonelist[0], ps);
                            return answer;
                        }
                    }
                    else
                    {
                        // 针对某一款手机
                        if (userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.DEBUGGING &&
                            userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.LEARNING &&
                            userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.START &&
                            userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.END)
                        {
                            if (userSession.liveTable.TalkingAbout != null)
                            {
                                userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;
                                string answer = "这手机";
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
                        }
                    }

                    if (userSession.liveTable.TalkingAbout.Model != null)
                    {
                        List<string> ps = new List<string>();
                        for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);

                        userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;

                        string answer = "这款手机";

                        for (int i = 0; i < info.items.Count; i++)
                        {
                            string value = userSession.liveTable.TalkingAbout.GetType().GetProperty
                                (Code.GlobalHash.SemanticToEN[info.items[i].name].ToString()).GetValue
                                (userSession.liveTable.TalkingAbout, null).ToString();

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

                        return answer;

                    }
                }
                else
                {
                    string answer = "";
                    List<Models.CellPhone> list = new List<Models.CellPhone>();
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
                
            }
            return "";
        }
    }
}