using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 正反问+值未定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input">有没有其他品牌的手机？</param>
    /// <param name="info">品牌：null, isNegative：true</param>
    /// <returns></returns>
    public class NEWSTP6 : ISTP
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

            bool isDirect = false; //是不是直接问一款准确的手机型号
            List<Models.CellPhone> list = new List<Models.CellPhone>();
            foreach (var it in info.items)
            {
                if (String.IsNullOrEmpty(info.conflictFeedback) && it.name.Equals("型号"))
                {
                    if (it.items.Count > 0)
                        isDirect = true;
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
                    if (userSession.liveTable.TalkingAbout.Model != null)
                    {
                        //  有没有其他颜色的？
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






        //{
        //    if (userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.DEBUGGING &&
        //    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.LEARNING &&
        //    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.START &&
        //    userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.END)
        //    {
        //        /* 可能用户会说：这款手机能不能用WIFI？
        //         * 这可以从TalkingAbout找相应的描述返回
        //         * */

        //        if (!String.IsNullOrEmpty(userSession.liveTable.TalkingAbout.Model))
        //        {
        //            List<string> ps = new List<string>();
        //            for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);

        //            userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;

        //            string answer = "这款手机";

        //            for (int i = 0; i < info.items.Count; i++)
        //            {
        //                string value = userSession.liveTable.TalkingAbout.GetType().GetProperty
        //                    (Code.GlobalHash.SemanticToEN[info.items[i].name].ToString()).GetValue
        //                    (userSession.liveTable.TalkingAbout, null).ToString();

        //                if (value != null)
        //                {
        //                    if (info.items[i].items.Count == 0)
        //                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, true) +
        //                            Code.Answering.getChangedStatement(info.items[i].name) + "。";
        //                    else
        //                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, true) +
        //                            info.items[i].items[0].rawData + "。";
        //                }
        //                else
        //                {
        //                    if (info.items[i].items.Count == 0)
        //                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, false) +
        //                            Code.Answering.getChangedStatement(info.items[i].name) + "。";
        //                    else
        //                        answer += Code.Answering.getAnswerStatusFromSentence(userSession, false) +
        //                            info.items[i].items[0].rawData + "。";
        //                }
        //            }

        //            return answer;
        //        }
        //    }
        //    return "";
        //}
    }
}