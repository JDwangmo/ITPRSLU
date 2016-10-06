using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    public class NEWSTP1 : ISTP
    {
        /// <summary>
        /// 特指问+值已定
        /// </summary>
        /// <param name="userSession"></param>
        /// <param name="input">有什么三星手机是2000块以下的？</param>
        /// <param name="info">品牌：三星；价钱：[0，2000]，块</param>
        /// <returns></returns>
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Extracting.ValidInfo info)
        {
            /* 分析：
             * 当手机型号被显式提出的情况一般在这里不会出现
             *--值已定
             *  --明确提出主语
             *  --没提出主语
             *      --有当前讨论的手机
             *      --没有当前讨论的手机
             *          --有候选手机
             *          --没有候选手机
             * */

            //用户希望系统根据当前句的语义和之前的收集的信息立即推荐一款手机 eg:能给我推荐一款三星手机吗？
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

            bool isDirect = false;
            List<Models.CellPhone> list = new List<Models.CellPhone>();
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
                if (userSession.liveTable.Candidates.Count > 0)
                {
                    list = Inquiry.MiniScreening(userSession.liveTable.Candidates, userSession.liveTable.Parameter, info);
                    userSession.liveTable.Candidates = list;
                }
                if (list.Count == 0)
                {
                    // 查询数据库，返回手机列表list
                    //####################这里的候选手机列表替换方式不是很妥当， 需要改
                    list = Inquiry.Screening(userSession.liveTable.Parameter, info);
                    userSession.liveTable.Candidates = list;
                }
                if (list.Count > 3)
                {
                    /* 如果筛选结果大于三款（且重要属性只有4个以下被确认），则进行主动引导
                     * 首先要先确定要引导的参数
                     * 然后还要记录当前状态变成主动引导
                     * 以及记录相关参数到live-table的RelativeParameter
                     * */
                    //string answear = "符合您要求的手机有以下几款：";
                    //for (int i = 0; i < 2; ++i)
                    //{
                    //    answear = answear + list[i].Model + "、";
                    //}
                    //answear = answear + list[2].Model + "等。";
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
                            return Answering.ActiveLeading(s);
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