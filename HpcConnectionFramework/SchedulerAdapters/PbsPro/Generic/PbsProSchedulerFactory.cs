using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    public class PbsProSchedulerFactory : SchedulerFactory
    {
        #region SchedulerFactory Members
        public override IRexScheduler CreateScheduler(Cluster configuration)
        {
            if (!linuxPbsSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
                linuxPbsSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(
                    GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
            return linuxPbsSchedulerSingletons[configuration.MasterNodeName];
        }

        protected override ISchedulerAdapter CreateSchedulerAdapter()
        {
            if (linuxPbsSchedulerAdapterInstance == null)
                linuxPbsSchedulerAdapterInstance = new PbsProSchedulerAdapter(CreateDataConvertor());
            return linuxPbsSchedulerAdapterInstance;
        }

        protected override ISchedulerDataConvertor CreateDataConvertor()
        {
            if (linuxPbsConvertorSingleton == null)
                linuxPbsConvertorSingleton = new PbsProDataConvertor(new PbsProConversionAdapterFactory());
            return linuxPbsConvertorSingleton;
        }

        protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration)
        {
            if (!linuxPbsConnectorSingletons.ContainsKey(configuration.MasterNodeName))
                linuxPbsConnectorSingletons[configuration.MasterNodeName] = new SshConnector();
            return linuxPbsConnectorSingletons[configuration.MasterNodeName];
        }
        #endregion

        #region Instance Fields
        private readonly Dictionary<string, IPoolableAdapter> linuxPbsConnectorSingletons = new Dictionary<string, IPoolableAdapter>();
        private readonly Dictionary<string, IRexScheduler> linuxPbsSchedulerSingletons = new Dictionary<string, IRexScheduler>();
        private ISchedulerDataConvertor linuxPbsConvertorSingleton;
        private ISchedulerAdapter linuxPbsSchedulerAdapterInstance;
        #endregion
    }
}