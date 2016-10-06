using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 选择问+值未定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public class NEWSTP8 : ISTP
    {
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Extracting.ValidInfo info)
        {
            //用户希望系统根据当前上下文推荐一个符合其他要求的基础上的手机属性
            if (Regex.IsMatch(input, @"推荐"))
            {
                if (userSession.liveTable.Candidates.Count == 0)
                {
                    userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.NOTFOUND;
                    return "不好意思，目前没有符合您要求的手机, 所以没有可以推荐的内容。我们会尽快收录。麻烦您改一下之前提出的条件吧。";
                }
                else
                {
                    // 目前推荐属性只支持单个属性的推荐
                    Models.CellPhone phone = userSession.liveTable.Candidates[0];
                    string retValue = "";
                    Type type = phone.GetType();
                    PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo pi in props)
                    {
                        string value1 = Convert.ToString(pi.GetValue(phone, null));
                        string name = pi.Name;
                        if (GlobalHash.SemanticToCN[name].Equals(info.items[0].name))
                        {
                            retValue = value1;
                            break;
                        }
                    }
                    if (Regex.IsMatch(retValue, @"^\d+$") && !info.items[0].name.Equals("型号"))
                    {
                        List<Code.Extracting.InfoUnit> temp = new List<Extracting.InfoUnit>();
                        temp.Add(new Code.Extracting.InfoUnit(retValue, string.Format("{0},{1}", Convert.ToDouble(retValue) * 0.99, Convert.ToDouble(retValue) * 1.01)));
                        Code.Extracting.InfoBlock oneblock = new Extracting.InfoBlock(info.items[0].name, "or", temp);
                        userSession.liveTable.Parameter[info.items[0].name] = oneblock;
                    }
                    else
                    {
                        if (retValue.IndexOf("|") >= 0)
                            retValue = retValue.Split(new char[] { '|' })[0];
                        List<Code.Extracting.InfoUnit> temp = new List<Extracting.InfoUnit>();
                        temp.Add(new Code.Extracting.InfoUnit(retValue, retValue));
                        Code.Extracting.InfoBlock oneblock = new Extracting.InfoBlock(info.items[0].name, "or", temp);
                        userSession.liveTable.Parameter[info.items[0].name] = oneblock;
                    }
                    return retValue + "是个不错的选择。";
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

                if (!String.IsNullOrEmpty(userSession.liveTable.TalkingAbout.Model))
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
            return "";
        }
    }
}