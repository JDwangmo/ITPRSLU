/*
 * author : xm
 * date : 2013/8/9
 * */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 陈述句
    /// </summary>
    public class STP0 : ISTP
    {
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Code.Extracting.ValidInfo info)
        {
            /* 分析：
             * 对于有参数的陈述句，其实有很多种情况：
             * 1、我要三星的手机
             * 2、这款Lumia520不错
             * 3、我觉得诺基亚比HTC好
             * 4、。。。
             * 不过我们从简处理，根据状态来进行处理
             * 如果当前状态是：主动引导，筛选请求
             * 则对于普通参数信息，比如2000块以下，大屏幕，直接更新到live-table
             * 对于比较类型参数信息，比如屏幕大一点，需要分析是普通意义上的大一点还是跟原来的比较
             * 然后就根据live-table在数据库里筛选手机
             * 接着根据筛选到的手机，如果大于3款，则进行主动提问，如果小于一款，则回答没有相应的手机，高级一点的话可以推荐一款最符合的
             * 如果1-3款，则给出筛选到的手机
             * 而如果当前状态为推荐、理解回答等的话，则另外处理
             * */

            if (userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.DEBUGGING &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.LEARNING &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.START &&
                userSession.liveTable.CurrentMode != Areas.Chat.Models.LiveTable.ModeType.END)
            {
                // 查询数据库，返回手机列表list
                List<Models.CellPhone> list = Inquiry.Screening(userSession.liveTable.Parameter, info);
                userSession.liveTable.Candidates = list;

                if (Regex.IsMatch(input, @"(不.*要)|(不要.*)"))
                {
                    bool negFigureInfoFound = false;
                    foreach(var di in info.items)
                    {
                        if (di.name == "价格" || di.name == "主屏尺寸")
                        {
                            foreach (var dii in di.items)
                            {
                                if (dii.isNegative)
                                {
                                    negFigureInfoFound = true;
                                }
                            }
                        }
                    }
                    //当用户表达抛弃属性时，将属性抛弃
                    if (!negFigureInfoFound && info.items.Count > 0 && info.items[0].items.Count > 0)
                    {
                        userSession.liveTable.Parameter[info.items[0].name] = null;
                        return "好的，请问您还有什么要求？";
                    }
                }
                else if (Regex.IsMatch(input, @"推荐"))
                {
                    //用户希望系统根据当前句的语义和之前的收集的信息立即推荐一款手机
                    if (!info.isInfoExist)
                    {
                        if (userSession.liveTable.Candidates.Count == 0)
                            Answering.NotFound(userSession, info);  // 状态CurrentMode在该方法中变更
                        else
                        {
                            userSession.liveTable.TalkingAbout = userSession.liveTable.Candidates[0];
                            userSession.liveTable.Candidates.Remove(userSession.liveTable.TalkingAbout);
                            userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                            return userSession.liveTable.TalkingAbout.Brand + userSession.liveTable.TalkingAbout.Model + "这款不错，你可以了解一下。";
                        }
                    }
                    else
                    {
                        if (info.AttributeValue)
                        {
                            userSession.liveTable.TalkingAbout = userSession.liveTable.Candidates[0];
                            userSession.liveTable.Candidates.Remove(userSession.liveTable.TalkingAbout);
                            userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                            return userSession.liveTable.TalkingAbout.Brand + userSession.liveTable.TalkingAbout.Model + "这款不错，你可以了解一下。";
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
                }
                else
                {
                    // 用户直接用陈述词询问手机信息 如：价钱   这时如果有当前讨论手机机器人应该回答该手机的价钱
                    if (userSession.liveTable.TalkingAbout != null)
                        if (info.items[0].items.Count == 0)
                        {
                            Type type = userSession.liveTable.TalkingAbout.GetType();
                            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (PropertyInfo pi in props)
                            {
                                object value1 = pi.GetValue(userSession.liveTable.TalkingAbout, null);
                                string name = pi.Name;
                                if (info.items[0].name.Equals(Code.GlobalHash.SemanticToCN[name]))
                                {
                                    return Convert.ToString(value1);
                                }
                            }
                        }
                }

                if (list.Count > 3)
                {
                    /* 如果筛选结果大于三款（且重要属性只有4个以下被确认），则进行主动引导
                     * 首先要先确定要引导的参数
                     * 然后还要记录当前状态变成主动引导
                     * 以及记录相关参数到live-table的RelativeParameter
                     * */
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

                /* 这里会有什么情况呢？
                 * 可能用户会说：我想了解一下它的优点
                 * 这可以从TalkingAbout找相应的描述返回
                 * 当然也可能还会说：我想要800万像素的
                 * 妈呀，这是重新筛选的节奏吗？
                 * 好吧，那勉强理解为用户是问：这款是800万像素的吗？
                 * 因为如果不是，则加上这个条件在数据库里也找不到，干脆就直接判断算了
                 * 其实不完全是这样的，因为候选机中可能有些满足要求，好吧，那就判断一下所有候选机吧=.=
                 * 
                 * 感觉要区分这两种句型就很麻烦了，特判有意思吗？
                 * 不过总比返回空串好，于是这里估计要写一堆了。。。
                 * */

                if (Regex.IsMatch(input, "了解一下|介绍一下|说一下|介绍下|说下|了解下"))
                {
                    if(userSession.liveTable.TalkingAbout != null){
                        List<string> ps = new List<string>();
                        for(int i = 0; i < info.items.Count; i ++) ps.Add(info.items[i].name);
                        userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.DETAIL;
                        return Answering.Statement(userSession.liveTable.TalkingAbout, ps);
                    }
                }
                // 第二种情况略复杂暂时不处理了
            }
            return "";
        }
    }
}
