using System;
using System.IO;
using System.Xml.Serialization;

namespace ConsoleAppGateway
{
    public static class AppConfig
    {
        private static AppProperties property = new AppProperties();
        private static string LocalFolder => AppDomain.CurrentDomain.BaseDirectory;
        public static AppProperties Property { get => property; set => property = value; }
        public static void Save()
        {
            var ser = new XmlSerializer(typeof(AppProperties));
            Stream st = File.Create(Path.Combine(LocalFolder, "Setting.xml"));
            ser.Serialize(st, Property);
            st.Close();
        }
        public static void Load()
        {
            if (File.Exists(Path.Combine(LocalFolder, "Setting.xml")))
            {
                var ser = new XmlSerializer(typeof(AppProperties));
                Stream st = File.OpenRead(Path.Combine(LocalFolder, "Setting.xml"));
                Property = (AppProperties)ser.Deserialize(st);
                st.Close();
            }
        }
        static AppConfig()
        {
            Load();
        }
    }
}
