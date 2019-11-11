using JarvisModuleCore.Attributes;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JarvisGroupButlerModule
{
    [JarvisModule]
    public class GroupButlerModule : JarvisModule
    {
        public override string Id => "jarvis.official.groupbutler";
        public override string Name => "Group butler";
        public override Version Version => Version.Parse("1.0.0");
        private const string mlDataFilePath = "Training\\data.json";
        public override TaskPredictionInput[] MLTrainingData => JsonConvert.DeserializeObject<TaskPredictionInput[]>(File.ReadAllText(mlDataFilePath));
    }
}
