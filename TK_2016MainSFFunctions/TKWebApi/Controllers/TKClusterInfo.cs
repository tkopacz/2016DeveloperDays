using System.Collections.Generic;
using System.Fabric.Description;
using System.Fabric.Health;
using System.Fabric.Query;

namespace TKWeb.Controllers {
    public class TKClusterInfo {
        public ApplicationHealth ApplicationHealth { get; internal set; }
        public string ApplicationManifest { get; internal set; }
        public ApplicationList AppList { get; internal set; }
        public ClusterHealth ClusterHealth { get; internal set; }
        public string ClusterManifest { get; internal set; }
        public NodeList NodeList { get; internal set; }
        public List<ServiceDescription> ServiceDescription { get; internal set; }
    }
}