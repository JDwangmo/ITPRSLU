using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 是非问+值未定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input">有小米2s吗？</param>
    /// <param name="info">品牌：小米2s</param>
    /// <returns></returns>
    public class NEWSTP4 : ISTP
    {
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Extracting.ValidInfo info)
        {
            //用户希望系统根据当前上下文推荐一个符合其他要求的基础上的手机属性 eg:能不能推荐一种颜色给我？
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

            if (info.items.Count > 1)
            {
                // 诺基亚1200有什么颜色的？
                Areas.Chat.Models.LiveTable liveTableTemp = new Areas.Chat.Models.LiveTable();
                List<string> ps = new List<string>();
                List<Models.CellPhone> phonelist = Inquiry.Screening(liveTableTemp.Parameter, info);
                for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                string answer = Answering.Statement(phonelist[0], ps);
                if (input.IndexOf("有") != -1)
                {
                    answer = answer.Replace("是", "有");
                }
                return answer;
            }
            else
            {
                // 已经推荐了一款手机，用户问“有什么颜色”
                if (userSession.liveTable.CurrentMode == Areas.Chat.Models.LiveTable.ModeType.RECOMMEND ||
                    userSession.liveTable.CurrentMode == Areas.Chat.Models.LiveTable.ModeType.DETAIL)
                {
                    if (userSession.liveTable.TalkingAbout != null)
                    {
                        List<string> ps = new List<string>();
                        for (int i = 0; i < info.items.Count; i++) ps.Add(info.items[i].name);
                        userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;
                        string answer = Answering.Statement(userSession.liveTable.TalkingAbout, ps);
                        if (input.IndexOf("有") != -1)
                        {
                            answer = answer.Replace("是", "有");
                        }
                        return answer;
                    }
                }
                else
                {
                    // 查询数据库，返回手机列表list
                    List<Models.CellPhone> list = Inquiry.Screening(userSession.liveTable.Parameter, info);
                    userSession.liveTable.Candidates = list;
                    if (list.Count > 3)
                    {
                        /* 如果筛选结果大于三款（且重要属性只有4个以下被确认），则进行主动引导
                         * 首先要先确定要引导的参数
                         * 然后还要记录当前状态变成主动引导
                         * 以及记录相关参数到live-table的RelativeParameter
                         * */
                        string answear = "符合您要求的手机有以下几款：";
                        for (int i = 0; i < 2; ++i)
                        {
                            answear = answear + list[i].Brand + list[i].Model + "、";
                        }
                        answear = answear + list[2].Brand + list[2].Model + "等。";
                        int recordedImportantParameter = 0;  //如果重要属性已有4个以上被确认，直接进行推荐，不继续引导
                        foreach (string s in Code.GlobalHash.ImportantParameter)
                        {
                            if (recordedImportantParameter >= 4)
                            {
                                userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                                userSession.liveTable.TalkingAbout = list[0];
                                return Answering.Recommand(list[0]);
                            }

                            if (!userSession.liveTable.Parameter.ContainsKey(s))
                            {
                                userSession.liveTable.RelatedParameter = s;
                                userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.ACTIVE;
                                return answear + Answering.ActiveLeading(s);
                            }
                            else
                            {
                                recordedImportantParameter++;
                            }
                        }
                    }
                    else if (list.Count <= 0)
                    {
                        return Answering.NotFound(userSession, info);  // 状态CurrentMode在该方法中变更
                    }
                    else
                    {
                        userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                        userSession.liveTable.TalkingAbout = list[0];
                        return Answering.Recommand(list[0]);
                    }
                }
                }
                

            return "";
        }
    }
}