/*
* ==============================================================================
* Filename: LuaExport
* Created:  2018/7/13 14:29:22
* Author:   エル・プサイ・コングリィ
* Purpose:  
* ==============================================================================
*/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MikuLuaProfiler
{
    public class Sample
    {
        public long realCurrentLuaMemory;
        public long currentLuaMemory;
        private string _fullName = null;
        public float currentTime;

        public string name;
        public float costTime;
        public long costGC;
        public Sample _father;
        public List<Sample> childs = new List<Sample>(256);

        #region property
        public string fullName
        {
            get
            {
                if (_father == null) return name;

                if (_fullName == null)
                {
                    Dictionary<string, string> childDict;
                    if (!m_fullNamePool.TryGetValue(_father.fullName, out childDict))
                    {
                        childDict = new Dictionary<string, string>();
                        m_fullNamePool.Add(_father.fullName, childDict);
                    }

                    if (!childDict.TryGetValue(name, out _fullName))
                    {
                        string value = name;
                        var f = _father;
                        while (f != null)
                        {
                            value = f.name + value;
                            f = f.fahter;
                        }
                        _fullName = value;
                        childDict[name] = _fullName;
                    }

                    return _fullName;
                }
                else
                {
                    return _fullName;
                }
            }
        }
        public Sample fahter
        {
            set
            {
                _father = value;
                if (_father != null)
                {
                    _father.childs.Add(this);
                }
            }
            get
            {
                return _father;
            }
        }
        #endregion

        #region pool
        private static Dictionary<string, Dictionary<string, string>> m_fullNamePool = new Dictionary<string, Dictionary<string, string>>();
        private static ObjectPool<Sample> samplePool = new ObjectPool<Sample>(250);
        public static Sample Create(float time, long memory, string name)
        {
            Sample s = samplePool.GetObject();
            s.currentTime = time;
            s.currentLuaMemory = memory;
            s.realCurrentLuaMemory = memory;
            s.costGC = 0;
            s.name = name;
            s.costTime = 0;
            s._fullName = null;
            s._father = null;
            s.childs.Clear();

            return s;
        }

        public void Restore()
        {
            for (int i = 0, imax = childs.Count; i < imax; i++)
            {
                childs[i].Restore();
            }
            samplePool.Store(this);
        }
        #endregion

        #region method
        public Sample Clone()
        {
            Sample s = new Sample();
            s.costGC = costGC;
            s.name = name;
            s.costTime = costTime;

            int childCount = childs.Count;
            for (int i = 0; i < childCount; i++)
            {
                Sample child = childs[i].Clone();
                child.fahter = s;
            }

            return s;
        }
        #endregion

        #region 序列化
        public static byte[] SerializeList(List<Sample> samples)
        {
            byte[] result = null;
            MemoryStream ms = new MemoryStream();
            BinaryWriter b = new BinaryWriter(ms);

            b.Write(samples.Count);
            for (int i = 0, imax = samples.Count; i < imax; i++)
            {
                byte[] datas = samples[i].Serialize();
                b.Write(datas.Length);
                b.Write(datas);
            }
            result = ms.ToArray();
            b.Close();

            return result;
        }

        public static List<Sample> DeserializeList(byte[] datas)
        {
            MemoryStream ms = new MemoryStream(datas);
            BinaryReader b = new BinaryReader(ms);

            int count = b.ReadInt32();
            List<Sample> result = new List<Sample>(count);

            for (int i = 0, imax = count; i < imax; i++)
            {
                int len = b.ReadInt32();
                Sample s = Deserialize(b.ReadBytes(len));
                result.Add(s);
            }
            b.Close();

            return result;
        }

        public byte[] Serialize()
        {
            byte[] result = null;
            MemoryStream ms = new MemoryStream();
            BinaryWriter b = new BinaryWriter(ms);

            b.Write(costGC);

            byte[] datas = Encoding.UTF8.GetBytes(name);
            b.Write(datas.Length);
            b.Write(datas);

            b.Write(costTime);

            b.Write(childs.Count);
            for (int i = 0, imax = childs.Count; i < imax; i++)
            {
                datas = childs[i].Serialize();
                b.Write(datas.Length);
                b.Write(datas);
            }
            result = ms.ToArray();
            b.Close();

            return result;
        }

        public static Sample Deserialize(byte[] data)
        {
            Sample s = new Sample();
            MemoryStream ms = new MemoryStream(data);
            BinaryReader b = new BinaryReader(ms);

            s.costGC = b.ReadInt64();

            int len = b.ReadInt32();
            byte[] datas = b.ReadBytes(len);
            s.name = Encoding.UTF8.GetString(datas);

            s.costTime = b.ReadSingle();

            int childCount = b.ReadInt32();
            for (int i = 0; i < childCount; i++)
            {
                len = b.ReadInt32();
                datas = b.ReadBytes(len);
                Sample child = Deserialize(datas);
                child.fahter = s;
            }

            b.Close();

            return s;
        }
        #endregion

    }

}
#endif

