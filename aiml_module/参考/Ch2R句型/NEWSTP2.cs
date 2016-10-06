using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ch2R.Code.STPP
{
    /// <summary>
    /// 特指问+值未定
    /// </summary>
    /// <param name="userSession"></param>
    /// <param name="input">三星手机有什么价位的？</param>
    /// <param name="info">品牌：三星；价钱-价位</param>
    /// <returns></returns>
    public class NEWSTP2 : ISTP
    {
        public string Process(Areas.Chat.Models.UserSession userSession, string input, Extracting.ValidInfo info)
        {

            /* 分析：
             * 当手机型号被显式提出，则以提出的手机型号为临时目标
             *--值未定
             *  --明确提出主语
             *  --没提出主语
             *      --有当前讨论的手机
             *      --没有当前讨论的手机
             *          --有候选手机
             *          --没有候选手机
             * */


            //用户希望系统根据当前上下文推荐一个符合其他要求的基础上的手机属性 eg:能推荐一种颜色给我吗？
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

            bool isDirect = false;
            List<Models.CellPhone> list = new List<Models.CellPhone>();
            string focusParameter = "";
            foreach (var it in info.items)
            {
                if (String.IsNullOrEmpty(info.conflictFeedback) && it.name.Equals("型号"))
                {
                    if (it.items.Count > 0)
                        isDirect = true;
                }
                if (it.isFocus)
                {
                    focusParameter = it.name;
                }
            }
            if (!isDirect)
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
                // 正在进行主动引导，用户问“有什么品牌”
                else if (userSession.liveTable.CurrentMode == Areas.Chat.Models.LiveTable.ModeType.ACTIVE ||
                    userSession.liveTable.CurrentMode == Areas.Chat.Models.LiveTable.ModeType.NOTFOUND ||
                    userSession.liveTable.CurrentMode == Areas.Chat.Models.LiveTable.ModeType.DAILYTALK)
                {
                    List<Models.CellPhone> phonelist = Inquiry.Screening(userSession.liveTable.Parameter, info);
                    Queue<String> queue = new Queue<String>();
                    // TODO
                    if (focusParameter == "价格")
                    {
                        decimal? min = 99999, max = -1;
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (phonelist[i].Price > max)
                                max = phonelist[i].Price;
                            if (phonelist[i].Price < min)
                                min = phonelist[i].Price;
                        }
                        if (min != max)
                        {
                            return "价格有" + (int)min + "的和" + (int)max + "元的，不知您的预算如何？";
                        }
                    }
                    else if (focusParameter == "后置摄像头像素")
                    {
                        decimal? min = 99999, max = -1;
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (phonelist[i].RearCameraPixel > max)
                                max = phonelist[i].RearCameraPixel;
                            if (phonelist[i].RearCameraPixel < min)
                                min = phonelist[i].RearCameraPixel;
                        }
                        if (min != max)
                        {
                            return "从" + (int)min + "万和" + (int)max + "万像素" + "，您都可以选择。";
                        }
                    }
                    else if (focusParameter == "主屏尺寸")
                    {
                        decimal? min = 99999, max = -1;
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (phonelist[i].ScreenSize > max)
                                max = phonelist[i].ScreenSize;
                            if (phonelist[i].ScreenSize < min)
                                min = phonelist[i].ScreenSize;
                        }
                        if (min != max)
                        {
                            return "屏幕最小是" + ((double)min).ToString("0.0") + "寸" + "，而最大的是"
                                + ((double)max).ToString("0.0") + "寸" + "，看您喜欢咯～";
                        }
                    }
                    else if (focusParameter == "CPU频率")
                    {
                        decimal? min = 99999, max = -1;
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (phonelist[i].CpuFrequency > max)
                                max = phonelist[i].CpuFrequency;
                            if (phonelist[i].CpuFrequency < min)
                                min = phonelist[i].CpuFrequency;
                        }
                        if (min != max)
                        {
                            return "频率最小是" + ((double)min).ToString("0.0") + "GHz" + "，而最大的是"
                                + ((double)max).ToString("0.0") + "GHz。";
                        }
                    }
                    else if (focusParameter == "CPU核数")
                    {
                        decimal? min = 99999, max = -1;
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (phonelist[i].CpuCoreNumber > max)
                                max = phonelist[i].CpuCoreNumber;
                            if (phonelist[i].CpuCoreNumber < min)
                                min = phonelist[i].CpuCoreNumber;
                        }
                        if (min != max)
                        {
                            return "核数最小是" + ((double)min).ToString("0.0") + "核" + "，而最大的是"
                                + ((double)max).ToString("0.0") + "核。";
                        }
                    }
                    else if (focusParameter == "品牌")
                    {
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (!queue.Contains(phonelist[i].Brand))
                            {
                                queue.Enqueue(phonelist[i].Brand);
                            }
                        }
                        if (queue.Count > 0)
                        {
                            String tmpStr = "";
                            if (queue.Count >= 5)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    tmpStr += queue.Dequeue() + "、";
                                }
                            }
                            else
                            {
                                for (int i = 0; i < queue.Count; i++)
                                {
                                    tmpStr += queue.Dequeue() + "、";
                                }
                            }
                            if (tmpStr[tmpStr.Length-1] == '|')
                                tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
                            return "有" + tmpStr + "等几个品牌可供选择。";
                        }
                    }
                    else if (focusParameter == "颜色")
                    {
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (!queue.Contains(phonelist[i].Color))
                            {
                                queue.Enqueue(phonelist[i].Color);
                            }
                        }
                        if (queue.Count > 0)
                        {
                            String tmpStr = "";
                            if (queue.Count >= 5)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    tmpStr += queue.Dequeue().Split(new char[] {'|'})[0] + "、";
                                }
                            }
                            else
                            {
                                for (int i = 0; i < queue.Count; i++)
                                {
                                    tmpStr += queue.Dequeue().Split(new char[] {'|'})[0] + "、";
                                }
                            }
                            if (tmpStr[tmpStr.Length - 1] == '、')
                                tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
                            return "有" + tmpStr + "等这几种颜色供你选择。";
                        }
                    }
                    else if (focusParameter == "型号")
                    {
                        for (int i = 0; i < phonelist.Count; i++)
                        {
                            if (!queue.Contains(phonelist[i].Model))
                            {
                                queue.Enqueue(phonelist[i].Brand);
                                queue.Enqueue(phonelist[i].Model);
                            }
                        }
                        //在向用户反馈型号信息的时候，用户看到只有一款准确型号的手机，有可能将其当作可以询问的当前手机
                        //例子 user：有什么型号的？
                        //     Ch2R：有 XXX 等几款手机。
                        //用户会将 XXX 手机当作 talkingabout的手机。
                        //所以在这里将反馈给用户的手机当作talkabout
                        //并且将状态改为推荐状态
                        if (phonelist.Count == 1)
                        {
                            userSession.liveTable.TalkingAbout = phonelist[0];
                            userSession.liveTable.CurrentMode = Areas.Chat.Models.LiveTable.ModeType.RECOMMEND;
                        }
                        if (queue.Count > 0)
                        {
                            String tmpStr = "";
                            if (queue.Count >= 10)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    tmpStr += queue.Dequeue() + queue.Dequeue() + "、";

                                }
                            }
                            else
                            {
                                for (int i = 0; i < queue.Count; i++)
                                {
                                    tmpStr += queue.Dequeue() + queue.Dequeue() + "、";
                                }
                            }
                            if (tmpStr[tmpStr.Length - 1] == '|')
                                tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
                            return "有" + tmpStr + "等几种型号的手机供你选择。";
                        }
                    }
                    else
                    {

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
                    for (int i = 0; i < info.items.Count; i++)
                    {
                        if (info.items[i].isFocus)
                        {
                            ps.Add(info.items[i].name);
                        }
                    }
                    return Answering.Statement(list[0], ps);
                }
            }

            // 查询数据库，返回手机列表list
            list = Inquiry.Screening(userSession.liveTable.Parameter, info);
            userSession.liveTable.Candidates = list;
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

            return "";
        }
    }
}