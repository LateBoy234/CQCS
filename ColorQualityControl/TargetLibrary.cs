using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ThreeNH.DataModel;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    /// <summary>
    /// 存样库类
    /// </summary>
    public class TargetLibrary
    {
        /// <summary>
        /// 样品库
        /// </summary>
//        private readonly Dictionary<string, StandardSpecimen> _targets = new Dictionary<string, StandardSpecimen>();
//        private Database _db;


        public TargetLibrary(string libraryFileName = "")
        {
            FileName = libraryFileName;
            if (!string.IsNullOrWhiteSpace(libraryFileName))
            {
                Name = Path.GetFileNameWithoutExtension(libraryFileName);
            }

            Targets = new Lazy<Dictionary<string, Specimen>>(()=>LoadTargets());
        }

        /// <summary>
        /// 存样库名
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// 存样库路径
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 存样库的数据
        /// </summary>
        public Lazy<Dictionary<string, Specimen>> Targets { get; private set; }

        private Dictionary<string, Specimen> LoadTargets()
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                return new Dictionary<string, Specimen>();
            }

            try
            {
//                _db = new Database(FileName);

                var targets = new Dictionary<string, Specimen>();
//                foreach (var color in _db.GetTargetColors(StandardIlluminant.D65, StandardObserver.CIE1964))
//                {
//                    targets.Add(color.Number, color.ToSpecimen());
//                }

                return targets;
            }
            catch (Exception e)
            {
                Trace.TraceWarning($"Load target libarary from '{FileName}' failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载存样库数据
        /// </summary>
        public void Load()
        {
            
        }

        public static bool IsNameValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return name.Intersect(Path.GetInvalidFileNameChars()).Count() == 0;
        }

        public static string GetPathByName(string libraryName)
        {
            return Path.Combine(App.LibraryPath, $"{libraryName}.db");
        }

        public void Rename(string newName)
        {
            string newFileName = GetPathByName(newName);
            if (File.Exists(FileName))
            {
                File.Move(FileName, newFileName);
            }

            FileName = newFileName;
            Name = newName;
        }

        public void Insert(List<StandardSpecimen> newSpecimens)
        {
//            _db.InsertTargetColors(newSpecimens);
        }

        public void Update(List<StandardSpecimen> specimens)
        {
//            _db.UpdateTargetColors(specimens);
        }

        public void RemoveAll(Predicate<Specimen> predicate)
        {
            
        }
    }
}
