using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AssetBundleFramework.Core
{
    public class Profiler
    {
        //打印函数耗时
        private static readonly Stopwatch ms_Stopwatch = Stopwatch.StartNew();
        private static readonly StringBuilder ms_StringBuilder = new StringBuilder();
        private static readonly List<Profiler> ms_Stack = new List<Profiler>();

        private List<Profiler> m_children;
        private string m_Name;
        private int m_Level;
        private long m_Timestamp;//时间戳
        private long m_Time;
        private int m_Count;

        //构造函数
        public Profiler(string name)
        {
            m_children = null;
            m_Name = name;
            m_Level = 0;
            m_Timestamp = -1;
            m_Time = 0;
            m_Count = 0;

        }

        //构造函数扩展
        private Profiler(string name,int level):this(name)//这样它会先执行上面的函数
        {
            m_Level = level;

        }

        public Profiler CreateChild(string name)
        {
            if(m_children == null)
            {
                m_children = new List<Profiler>();
            }

            Profiler profiler = new Profiler(name,m_Level+1);//不断的叠层
            m_children.Add(profiler);
            return profiler;//返回了创造的Children
        }

        public void Start()
        {
            //时间戳似乎必须从-1开始
            if (m_Timestamp != -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Start)} error，repeat start,name:{m_Name}");
            }

            m_Timestamp = ms_Stopwatch.ElapsedTicks;
        }

        public void Stop()
        {
            if (m_Timestamp == -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Start)} error，repeat stop,name:{m_Name}");
            }

            m_Time += ms_Stopwatch.ElapsedTicks - m_Timestamp; //计算耗时
            m_Count += 1;
            m_Timestamp = -1;
        }

        private void Format()
        {
            ms_StringBuilder.AppendLine();

            for(int i = 0; i < m_Level; ++i)
            {
                ms_StringBuilder.AppendLine(i < m_Level - 1 ? "|  " : "|--");
            }

            ms_StringBuilder.Append(m_Name);

            if(m_Count <= 0)
            {
                return;
            }

            ms_StringBuilder.Append(" [");
            ms_StringBuilder.Append("Count");
            ms_StringBuilder.Append(": ");
            ms_StringBuilder.Append(m_Count);
            ms_StringBuilder.Append(", ");
            ms_StringBuilder.Append("Time");
            ms_StringBuilder.Append(": ");

            ms_StringBuilder.Append($"{(float)m_Time / TimeSpan.TicksPerMillisecond}");
            ms_StringBuilder.Append("Millisecond ");

            ms_StringBuilder.Append($"{(float)m_Time / TimeSpan.TicksPerSecond}");
            ms_StringBuilder.Append("Second ");

            ms_StringBuilder.Append($"{(float)m_Time / TimeSpan.TicksPerMinute}");
            ms_StringBuilder.Append("Minute ");

            ms_StringBuilder.Append("]");

        }

        public override string ToString()
        {
            ms_StringBuilder.Clear();
            ms_Stack.Clear();
            ms_Stack.Add(this);

            //遍历这个Stack
            while (ms_Stack.Count > 0)
            {
                int index = ms_Stack.Count - 1;
                Profiler profiler = ms_Stack[index];
                ms_Stack.RemoveAt(index);

                profiler.Format();//规范输出?

                List<Profiler> children = profiler.m_children;
                if(children == null)
                {
                    continue;
                }

                for(int i = children.Count - 1; i >=0; --i)
                {
                    ms_Stack.Add(children[i]);
                }
            }

            return ms_StringBuilder.ToString();
        }
    }
}
