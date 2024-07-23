using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AssetBundleFramework.Core
{
    public class Profiler
    {
        //��ӡ������ʱ
        private static readonly Stopwatch ms_Stopwatch = Stopwatch.StartNew();
        private static readonly StringBuilder ms_StringBuilder = new StringBuilder();
        private static readonly List<Profiler> ms_Stack = new List<Profiler>();

        private List<Profiler> m_children;
        private string m_Name;
        private int m_Level;
        private long m_Timestamp;//ʱ���
        private long m_Time;
        private int m_Count;

        //���캯��
        public Profiler(string name)
        {
            m_children = null;
            m_Name = name;
            m_Level = 0;
            m_Timestamp = -1;
            m_Time = 0;
            m_Count = 0;

        }

        //���캯����չ
        private Profiler(string name,int level):this(name)//����������ִ������ĺ���
        {
            m_Level = level;

        }

        public Profiler CreateChild(string name)
        {
            if(m_children == null)
            {
                m_children = new List<Profiler>();
            }

            Profiler profiler = new Profiler(name,m_Level+1);//���ϵĵ���
            m_children.Add(profiler);
            return profiler;//�����˴����Children
        }

        public void Start()
        {
            //ʱ����ƺ������-1��ʼ
            if (m_Timestamp != -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Start)} error��repeat start,name:{m_Name}");
            }

            m_Timestamp = ms_Stopwatch.ElapsedTicks;
        }

        public void Stop()
        {
            if (m_Timestamp == -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Start)} error��repeat stop,name:{m_Name}");
            }

            m_Time += ms_Stopwatch.ElapsedTicks - m_Timestamp; //�����ʱ
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

            //�������Stack
            while (ms_Stack.Count > 0)
            {
                int index = ms_Stack.Count - 1;
                Profiler profiler = ms_Stack[index];
                ms_Stack.RemoveAt(index);

                profiler.Format();//�淶���?

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
