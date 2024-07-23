using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AssetBundleFramework.Editor
{
    public static class XmlUtility
    {
        //��xml
        public static T Read<T>(string fileName) where T : class
        {
            FileStream stream = null;
            if (!File.Exists(fileName))
            {
                return default(T);
            }

            try
            {
                //���л�
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                stream = File.OpenRead(fileName);
                XmlReader reader = XmlReader.Create(stream);
                //�����л�
                T instance = (T)serializer.Deserialize(reader);
                stream.Close();
                return instance;
            }
            catch
            {
                if(stream != null)
                {
                    stream.Close();

                }
                return default(T);
            }
        }
    }

}
